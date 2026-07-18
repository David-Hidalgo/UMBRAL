using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ScoringService.Domain.Entities;
using ScoringService.Domain.ValueObjects;
using ScoringService.Infrastructure.Persistence.Contexts;
using ScoringService.Infrastructure.Persistence.Inbox;

namespace ScoringService.Infrastructure.Messaging.Consumers
{
    /// <summary>
    /// Consume los eventos de sesión publicados por liveSessionService y mantiene el espejo de
    /// monitoreo (SessionMonitor) además del historial de auditoría (SessionEvent). Aplica el
    /// patrón Inbox para ser idempotente (backend-spec §4) y reintenta la conexión al broker.
    /// </summary>
    public sealed class SessionEventsConsumer : BackgroundService
    {
        private const string QueueName = "umbral.scoring.session-events";
        private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SessionEventsConsumer> _logger;
        private readonly RabbitMqOptions _options;
        private IConnection? _connection;
        private IChannel? _channel;

        public SessionEventsConsumer(
            IServiceProvider serviceProvider,
            ILogger<SessionEventsConsumer> logger,
            IOptions<RabbitMqOptions> options)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await ConnectWithRetryAsync(stoppingToken);

                await _channel!.ExchangeDeclareAsync(
                    exchange: _options.ExchangeName,
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false,
                    cancellationToken: stoppingToken);

                await _channel.QueueDeclareAsync(
                    queue: QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: stoppingToken);

                foreach (var routingKey in BoundRoutingKeys)
                {
                    await _channel.QueueBindAsync(
                        queue: QueueName,
                        exchange: _options.ExchangeName,
                        routingKey: routingKey,
                        cancellationToken: stoppingToken);
                }

                // Procesar un solo mensaje a la vez: garantiza el orden causal de los eventos
                // (p. ej. SessionCreated antes que TeamRegistered) y evita colisiones de escritura
                // concurrentes sobre el mismo SessionMonitor.
                await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

                _logger.LogInformation("scoringService registró el consumidor de eventos en la cola: {QueueName}", QueueName);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += async (_, ea) =>
                {
                    try
                    {
                        await HandleMessageAsync(ea, stoppingToken);
                        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error procesando evento con routingKey {RoutingKey}", ea.RoutingKey);
                        // Sin requeue para no ciclar infinitamente ante un mensaje corrupto.
                        await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                    }
                };

                await _channel.BasicConsumeAsync(queue: QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("SessionEventsConsumer de scoringService detenido.");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Fallo crítico en SessionEventsConsumer de scoringService.");
            }
        }

        private static string[] BoundRoutingKeys =>
        [
            nameof(SessionCreatedIntegrationEvent),
            nameof(TeamRegisteredIntegrationEvent),
            nameof(PlayerJoinedTeamIntegrationEvent),
            nameof(SessionActivatedIntegrationEvent),
            nameof(SessionPausedIntegrationEvent),
            nameof(SessionFinishedIntegrationEvent),
            nameof(SessionCancelledIntegrationEvent),
            nameof(StageTransitionedIntegrationEvent)
        ];

        private async Task ConnectWithRetryAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.Host,
                Port = _options.Port,
                UserName = _options.Username,
                Password = _options.Password
            };

            const int maxAttempts = 10;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    _connection = await factory.CreateConnectionAsync(stoppingToken);
                    _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
                    return;
                }
                catch (Exception ex) when (attempt < maxAttempts)
                {
                    _logger.LogWarning(ex, "No se pudo conectar a RabbitMQ (intento {Attempt}/{Max}); reintentando...", attempt, maxAttempts);
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                }
            }

            // Último intento: si falla, propaga y el servicio lo registra como fallo crítico.
            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
        }

        private async Task HandleMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            var body = ea.Body.ToArray();
            var routingKey = ea.RoutingKey;
            var message = Encoding.UTF8.GetString(body);

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ScoringDbContext>();

            // Inbox: descartar mensajes ya procesados (idempotencia — backend-spec §4).
            var dedupeId = DeterministicId(routingKey, body);
            if (await db.ProcessedEvents.AnyAsync(p => p.Id == dedupeId, cancellationToken))
            {
                _logger.LogDebug("Evento {RoutingKey} ya procesado; se omite.", routingKey);
                return;
            }

            var now = DateTime.UtcNow;
            await ApplyEventAsync(db, routingKey, message, now, cancellationToken);

            db.ProcessedEvents.Add(ProcessedEvent.Create(dedupeId, routingKey, now));
            await db.SaveChangesAsync(cancellationToken);
        }

        private async Task ApplyEventAsync(
            ScoringDbContext db, string routingKey, string message, DateTime now, CancellationToken cancellationToken)
        {
            switch (routingKey)
            {
                case nameof(SessionCreatedIntegrationEvent):
                    {
                        var e = Deserialize<SessionCreatedIntegrationEvent>(message);
                        if (e is null) return;
                        var existing = await db.SessionMonitors.FirstOrDefaultAsync(m => m.SessionId == e.SessionId, cancellationToken);
                        if (existing is null)
                        {
                            db.SessionMonitors.Add(SessionMonitor.Create(e.SessionId, e.MissionId, e.Name));
                            RecordAudit(db, e.SessionId, "SessionCreated", $"Sesión '{e.Name}' creada.", now);
                        }
                        break;
                    }
                case nameof(TeamRegisteredIntegrationEvent):
                    {
                        var e = Deserialize<TeamRegisteredIntegrationEvent>(message);
                        if (e is null) return;
                        var monitor = await db.SessionMonitors.FirstOrDefaultAsync(m => m.SessionId == e.SessionId, cancellationToken);
                        if (monitor is null)
                        {
                            _logger.LogWarning("TeamRegistered para sesión desconocida {SessionId}; se omite.", e.SessionId);
                            return;
                        }
                        monitor.RegisterTeam(e.TeamId, new TeamCode(e.TeamName));
                        RecordAudit(db, e.SessionId, "TeamRegistered", $"Equipo '{e.TeamName}' registrado.", now);
                        break;
                    }
                case nameof(PlayerJoinedTeamIntegrationEvent):
                    {
                        var e = Deserialize<PlayerJoinedTeamIntegrationEvent>(message);
                        if (e is null) return;
                        RecordAudit(db, e.SessionId, "PlayerJoinedTeam", $"'{e.DisplayName}' se unió al equipo {e.TeamId}.", now);
                        break;
                    }
                case nameof(SessionActivatedIntegrationEvent):
                    {
                        var e = Deserialize<SessionActivatedIntegrationEvent>(message);
                        if (e is null) return;
                        var monitor = await db.SessionMonitors.FirstOrDefaultAsync(m => m.SessionId == e.SessionId, cancellationToken);
                        if (monitor is null) return;
                        monitor.Activate(e.SubStageIds ?? new List<Guid>());
                        RecordAudit(db, e.SessionId, "SessionActivated", "La sesión pasó a estado Activa.", now);
                        break;
                    }
                case nameof(SessionPausedIntegrationEvent):
                    {
                        var e = Deserialize<SessionPausedIntegrationEvent>(message);
                        if (e is null) return;
                        var monitor = await db.SessionMonitors.FirstOrDefaultAsync(m => m.SessionId == e.SessionId, cancellationToken);
                        if (monitor is null) return;
                        monitor.Pause();
                        RecordAudit(db, e.SessionId, "SessionPaused", "La sesión pasó a estado Pausada.", now);
                        break;
                    }
                case nameof(SessionFinishedIntegrationEvent):
                    {
                        var e = Deserialize<SessionFinishedIntegrationEvent>(message);
                        if (e is null) return;
                        var monitor = await db.SessionMonitors.FirstOrDefaultAsync(m => m.SessionId == e.SessionId, cancellationToken);
                        if (monitor is null) return;
                        monitor.Finish();
                        RecordAudit(db, e.SessionId, "SessionFinished", "La sesión finalizó.", now);
                        break;
                    }
                case nameof(SessionCancelledIntegrationEvent):
                    {
                        var e = Deserialize<SessionCancelledIntegrationEvent>(message);
                        if (e is null) return;
                        var monitor = await db.SessionMonitors.FirstOrDefaultAsync(m => m.SessionId == e.SessionId, cancellationToken);
                        if (monitor is null) return;
                        monitor.Cancel();
                        RecordAudit(db, e.SessionId, "SessionCancelled", "La sesión fue cancelada.", now);
                        break;
                    }
                case nameof(StageTransitionedIntegrationEvent):
                    {
                        var e = Deserialize<StageTransitionedIntegrationEvent>(message);
                        if (e is null) return;
                        var monitor = await db.SessionMonitors.FirstOrDefaultAsync(m => m.SessionId == e.SessionId, cancellationToken);
                        if (monitor is null) return;
                        monitor.TransitionStage(e.NewSubStageIds ?? new List<Guid>());
                        RecordAudit(db, e.SessionId, "StageTransitioned", "La sesión avanzó de etapa.", now);
                        break;
                    }
            }
        }

        private static void RecordAudit(ScoringDbContext db, Guid sessionId, string type, string description, DateTime now)
            => db.SessionEvents.Add(SessionEvent.Record(Guid.NewGuid(), sessionId, type, description, now));

        private static T? Deserialize<T>(string message) => JsonSerializer.Deserialize<T>(message, SerializerOptions);

        private static Guid DeterministicId(string routingKey, byte[] body)
        {
            var prefix = Encoding.UTF8.GetBytes(routingKey + "|");
            var input = new byte[prefix.Length + body.Length];
            Buffer.BlockCopy(prefix, 0, input, 0, prefix.Length);
            Buffer.BlockCopy(body, 0, input, prefix.Length, body.Length);

            var hash = SHA256.HashData(input);
            var guidBytes = new byte[16];
            Array.Copy(hash, guidBytes, 16);
            return new Guid(guidBytes);
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }

        // Copias locales de los contratos de eventos publicados por liveSessionService.
        private sealed record SessionCreatedIntegrationEvent(Guid SessionId, Guid MissionId, string Name);
        private sealed record TeamRegisteredIntegrationEvent(Guid SessionId, Guid TeamId, string TeamName);
        private sealed record PlayerJoinedTeamIntegrationEvent(Guid SessionId, Guid TeamId, string KeycloakUserId, string DisplayName);
        private sealed record SessionActivatedIntegrationEvent(Guid SessionId, IReadOnlyList<Guid> SubStageIds);
        private sealed record SessionPausedIntegrationEvent(Guid SessionId);
        private sealed record SessionFinishedIntegrationEvent(Guid SessionId, IReadOnlyList<Guid> SubStageIds);
        private sealed record SessionCancelledIntegrationEvent(Guid SessionId, IReadOnlyList<Guid> SubStageIds);
        private sealed record StageTransitionedIntegrationEvent(Guid SessionId, IReadOnlyList<Guid> PreviousSubStageIds, IReadOnlyList<Guid> NewSubStageIds);
    }
}
