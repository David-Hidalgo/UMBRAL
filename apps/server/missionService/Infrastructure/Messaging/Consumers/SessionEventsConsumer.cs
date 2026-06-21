using System;
using System.Collections.Generic;
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
using MissionService.Infrastructure.Persistence;

namespace MissionService.Infrastructure.Messaging.Consumers
{
    public sealed class SessionEventsConsumer : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SessionEventsConsumer> _logger;
        private readonly RabbitMqOptions _options;
        private IConnection? _connection;
        private IChannel? _channel;
        private string? _queueName;

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
                var factory = new ConnectionFactory
                {
                    HostName = _options.Host,
                    Port = _options.Port,
                    UserName = _options.Username,
                    Password = _options.Password
                };

                _connection = await factory.CreateConnectionAsync(stoppingToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

                // Declarar exchange (debe coincidir con liveSessionService)
                await _channel.ExchangeDeclareAsync(
                    exchange: _options.ExchangeName,
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false,
                    cancellationToken: stoppingToken);

                // Declarar cola
                _queueName = "umbral.missions.session-events";
                await _channel.QueueDeclareAsync(
                    queue: _queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: stoppingToken);

                // Bindear eventos que nos interesan
                var routingKeys = new[]
                {
                    nameof(SessionActivatedIntegrationEvent),
                    nameof(SessionFinishedIntegrationEvent),
                    nameof(SessionCancelledIntegrationEvent),
                    nameof(StageTransitionedIntegrationEvent)
                };

                foreach (var key in routingKeys)
                {
                    await _channel.QueueBindAsync(
                        queue: _queueName,
                        exchange: _options.ExchangeName,
                        routingKey: key,
                        cancellationToken: stoppingToken);
                }

                _logger.LogInformation("Registrado consumidor de eventos de sesión en la cola: {QueueName}", _queueName);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += async (model, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        var routingKey = ea.RoutingKey;

                        _logger.LogInformation("Evento recibido en missionService: {RoutingKey}", routingKey);

                        await ProcessMessageAsync(routingKey, message, stoppingToken);

                        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error procesando evento de RabbitMQ con routingKey: {RoutingKey}", ea.RoutingKey);
                        // Nack con requeue = false en caso de fallo para no ciclar infinitamente
                        await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                    }
                };

                await _channel.BasicConsumeAsync(
                    queue: _queueName,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken);

                // Esperar a que se solicite la detención del servicio
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Servicio SessionEventsConsumer cancelado.");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Fallo crítico en SessionEventsConsumer.");
            }
        }

        private async Task ProcessMessageAsync(string routingKey, string message, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MissionDbContext>();

            var serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            if (routingKey == nameof(SessionActivatedIntegrationEvent))
            {
                var @event = JsonSerializer.Deserialize<SessionActivatedIntegrationEvent>(message, serializerOptions);
                if (@event?.SubStageIds != null)
                {
                    await UpdateSessionCountsAsync(dbContext, @event.SubStageIds, increment: true, cancellationToken);
                }
            }
            else if (routingKey == nameof(SessionFinishedIntegrationEvent))
            {
                var @event = JsonSerializer.Deserialize<SessionFinishedIntegrationEvent>(message, serializerOptions);
                if (@event?.SubStageIds != null)
                {
                    await UpdateSessionCountsAsync(dbContext, @event.SubStageIds, increment: false, cancellationToken);
                }
            }
            else if (routingKey == nameof(SessionCancelledIntegrationEvent))
            {
                var @event = JsonSerializer.Deserialize<SessionCancelledIntegrationEvent>(message, serializerOptions);
                if (@event?.SubStageIds != null)
                {
                    await UpdateSessionCountsAsync(dbContext, @event.SubStageIds, increment: false, cancellationToken);
                }
            }
            else if (routingKey == nameof(StageTransitionedIntegrationEvent))
            {
                var @event = JsonSerializer.Deserialize<StageTransitionedIntegrationEvent>(message, serializerOptions);
                if (@event != null)
                {
                    if (@event.PreviousSubStageIds != null && @event.PreviousSubStageIds.Count > 0)
                    {
                        await UpdateSessionCountsAsync(dbContext, @event.PreviousSubStageIds, increment: false, cancellationToken);
                    }
                    if (@event.NewSubStageIds != null && @event.NewSubStageIds.Count > 0)
                    {
                        await UpdateSessionCountsAsync(dbContext, @event.NewSubStageIds, increment: true, cancellationToken);
                    }
                }
            }
        }

        private async Task UpdateSessionCountsAsync(
            MissionDbContext dbContext,
            IReadOnlyList<Guid> subStageIds,
            bool increment,
            CancellationToken cancellationToken)
        {
            foreach (var subStageId in subStageIds)
            {
                var subStage = await dbContext.SubStages.FirstOrDefaultAsync(s => s.Id == subStageId, cancellationToken);
                if (subStage != null)
                {
                    if (increment)
                    {
                        subStage.IncrementSessionCount();
                        _logger.LogInformation("Incrementado ActiveSessionCount para SubStage {SubStageId}", subStageId);
                    }
                    else
                    {
                        try
                        {
                            subStage.DecrementSessionCount();
                            _logger.LogInformation("Decrementado ActiveSessionCount para SubStage {SubStageId}", subStageId);
                        }
                        catch (InvalidOperationException)
                        {
                            _logger.LogWarning("El ActiveSessionCount para SubStage {SubStageId} ya era cero.", subStageId);
                        }
                    }
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }

    public record SessionActivatedIntegrationEvent(Guid SessionId, IReadOnlyList<Guid> SubStageIds);
    public record SessionFinishedIntegrationEvent(Guid SessionId, IReadOnlyList<Guid> SubStageIds);
    public record SessionCancelledIntegrationEvent(Guid SessionId, IReadOnlyList<Guid> SubStageIds);
    public record StageTransitionedIntegrationEvent(Guid SessionId, IReadOnlyList<Guid> PreviousSubStageIds, IReadOnlyList<Guid> NewSubStageIds);
}
