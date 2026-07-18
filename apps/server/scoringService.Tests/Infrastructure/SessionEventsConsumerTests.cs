using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using ScoringService.Infrastructure.Messaging;
using ScoringService.Infrastructure.Messaging.Consumers;
using ScoringService.Infrastructure.Persistence.Contexts;
using Xunit;

namespace ScoringService.Tests.Infrastructure
{
    /// <summary>
    /// Pruebas unitarias para SessionEventsConsumer de scoringService.
    /// Se usa InMemory DB y reflexión para invocar HandleMessageAsync directamente
    /// sin necesidad de un broker RabbitMQ real.
    /// El DbContext se registra como Scoped factory para que cada llamada a
    /// HandleMessageAsync (que crea su propio IServiceScope) obtenga una instancia
    /// nueva pero sobre la misma base de datos en memoria.
    /// </summary>
    public class SessionEventsConsumerTests
    {
        // Nombres de eventos privados en el consumer (deben coincidir con los record internos)
        private const string SessionCreated = "SessionCreatedIntegrationEvent";
        private const string TeamRegistered = "TeamRegisteredIntegrationEvent";
        private const string PlayerJoined = "PlayerJoinedTeamIntegrationEvent";
        private const string SessionActivated = "SessionActivatedIntegrationEvent";
        private const string SessionPaused = "SessionPausedIntegrationEvent";
        private const string SessionFinished = "SessionFinishedIntegrationEvent";
        private const string SessionCancelled = "SessionCancelledIntegrationEvent";
        private const string StageTransitioned = "StageTransitionedIntegrationEvent";

        // DTOs locales que replican los records privados del consumer
        private sealed record SessionCreatedPayload(Guid SessionId, Guid MissionId, string Name);
        private sealed record TeamRegisteredPayload(Guid SessionId, Guid TeamId, string TeamName);
        private sealed record PlayerJoinedPayload(Guid SessionId, Guid TeamId, string KeycloakUserId, string DisplayName);
        private sealed record SessionActivatedPayload(Guid SessionId, IReadOnlyList<Guid> SubStageIds);
        private sealed record SessionPausedPayload(Guid SessionId);
        private sealed record SessionFinishedPayload(Guid SessionId, IReadOnlyList<Guid> SubStageIds);
        private sealed record SessionCancelledPayload(Guid SessionId, IReadOnlyList<Guid> SubStageIds);
        private sealed record StageTransitionedPayload(Guid SessionId, IReadOnlyList<Guid> PreviousSubStageIds, IReadOnlyList<Guid> NewSubStageIds);

        // ── Helpers ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Crea opciones de InMemory con un nombre único, compartido dentro de un test,
        /// para que distintos DbContext creados sobre esas opciones vean los mismos datos.
        /// </summary>
        private static DbContextOptions<ScoringDbContext> CreateInMemoryOptions(string dbName)
            => new DbContextOptionsBuilder<ScoringDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

        /// <summary>
        /// Registra el DbContext como Scoped factory (no como instancia singleton)
        /// para que cada scope interno creado por HandleMessageAsync construya su propia
        /// instancia y la pueda disponer sin afectar a otros scopes.
        /// </summary>
        private static ServiceProvider CreateServiceProvider(DbContextOptions<ScoringDbContext> options)
        {
            var services = new ServiceCollection();
            services.AddScoped<ScoringDbContext>(_ => new ScoringDbContext(options));
            return services.BuildServiceProvider();
        }

        private static SessionEventsConsumer CreateConsumer(IServiceProvider sp)
        {
            var options = Options.Create(new RabbitMqOptions
            {
                Host = "localhost",
                Port = 5672,
                Username = "guest",
                Password = "guest",
                ExchangeName = "umbral.livesessions",
                ScoringExchangeName = "umbral.scoring"
            });
            return new SessionEventsConsumer(sp, NullLogger<SessionEventsConsumer>.Instance, options);
        }

        private static async Task InvokeHandleMessageAsync(
            SessionEventsConsumer consumer, string routingKey, object payload)
        {
            var method = typeof(SessionEventsConsumer).GetMethod(
                "HandleMessageAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

            var body = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
            var ea = new BasicDeliverEventArgs(
                consumerTag: "test",
                deliveryTag: 1,
                redelivered: false,
                exchange: "test",
                routingKey: routingKey,
                properties: new BasicProperties(),
                body: body);

            await (Task)method.Invoke(consumer, new object[] { ea, CancellationToken.None })!;
        }

        // ── Tests ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task HandleMessageAsync_SessionCreated_CreatesSessionMonitor()
        {
            var dbName = Guid.NewGuid().ToString();
            var opts = CreateInMemoryOptions(dbName);
            using var sp = CreateServiceProvider(opts);
            var consumer = CreateConsumer(sp);

            var sessionId = Guid.NewGuid();
            await InvokeHandleMessageAsync(consumer, SessionCreated,
                new SessionCreatedPayload(sessionId, Guid.NewGuid(), "Test Session"));

            // Aserción con contexto separado sobre la misma DB en memoria
            await using var assertDb = new ScoringDbContext(opts);
            var monitor = await assertDb.SessionMonitors.FirstOrDefaultAsync(m => m.SessionId == sessionId);
            monitor.Should().NotBeNull();
        }

        [Fact]
        public async Task HandleMessageAsync_SessionCreated_IsIdempotent_WhenProcessedTwice()
        {
            var dbName = Guid.NewGuid().ToString();
            var opts = CreateInMemoryOptions(dbName);
            using var sp = CreateServiceProvider(opts);
            var consumer = CreateConsumer(sp);

            var sessionId = Guid.NewGuid();
            var payload = new SessionCreatedPayload(sessionId, Guid.NewGuid(), "Idempotent");

            await InvokeHandleMessageAsync(consumer, SessionCreated, payload);
            await InvokeHandleMessageAsync(consumer, SessionCreated, payload); // mismo body → mismo hash

            await using var assertDb = new ScoringDbContext(opts);
            var count = await assertDb.SessionMonitors.CountAsync(m => m.SessionId == sessionId);
            count.Should().Be(1, "el patrón Inbox debe descartar mensajes duplicados");
        }

        [Fact]
        public async Task HandleMessageAsync_TeamRegistered_RegistersTeamInMonitor()
        {
            var dbName = Guid.NewGuid().ToString();
            var opts = CreateInMemoryOptions(dbName);
            using var sp = CreateServiceProvider(opts);
            var consumer = CreateConsumer(sp);

            var sessionId = Guid.NewGuid();
            var teamId = Guid.NewGuid();

            await InvokeHandleMessageAsync(consumer, SessionCreated,
                new SessionCreatedPayload(sessionId, Guid.NewGuid(), "Session con equipo"));

            await InvokeHandleMessageAsync(consumer, TeamRegistered,
                new TeamRegisteredPayload(sessionId, teamId, "AlphaTeam"));

            await using var assertDb = new ScoringDbContext(opts);
            var monitor = await assertDb.SessionMonitors
                .Include(m => m.Teams)
                .FirstOrDefaultAsync(m => m.SessionId == sessionId);

            monitor.Should().NotBeNull();
            monitor!.Teams.Should().ContainSingle(t => t.TeamId == teamId);
        }

        [Fact]
        public async Task HandleMessageAsync_TeamRegistered_UnknownSession_DoesNotThrow()
        {
            var dbName = Guid.NewGuid().ToString();
            var opts = CreateInMemoryOptions(dbName);
            using var sp = CreateServiceProvider(opts);
            var consumer = CreateConsumer(sp);

            await FluentActions.Invoking(() =>
                InvokeHandleMessageAsync(consumer, TeamRegistered,
                    new TeamRegisteredPayload(Guid.NewGuid(), Guid.NewGuid(), "GhostTeam")))
                .Should().NotThrowAsync();
        }

        [Fact]
        public async Task HandleMessageAsync_PlayerJoinedTeam_RecordsAuditEvent()
        {
            var dbName = Guid.NewGuid().ToString();
            var opts = CreateInMemoryOptions(dbName);
            using var sp = CreateServiceProvider(opts);
            var consumer = CreateConsumer(sp);

            var sessionId = Guid.NewGuid();
            await InvokeHandleMessageAsync(consumer, SessionCreated,
                new SessionCreatedPayload(sessionId, Guid.NewGuid(), "Session Player"));

            await InvokeHandleMessageAsync(consumer, PlayerJoined,
                new PlayerJoinedPayload(sessionId, Guid.NewGuid(), "kc-123", "Juan"));

            await using var assertDb = new ScoringDbContext(opts);
            var events = await assertDb.SessionEvents
                .Where(e => e.SessionId == sessionId && e.Type == "PlayerJoinedTeam")
                .ToListAsync();
            events.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public async Task HandleMessageAsync_SessionActivated_RecordsAudit()
        {
            var dbName = Guid.NewGuid().ToString();
            var opts = CreateInMemoryOptions(dbName);
            using var sp = CreateServiceProvider(opts);
            var consumer = CreateConsumer(sp);

            var sessionId = Guid.NewGuid();
            await InvokeHandleMessageAsync(consumer, SessionCreated,
                new SessionCreatedPayload(sessionId, Guid.NewGuid(), "Active Session"));

            await InvokeHandleMessageAsync(consumer, SessionActivated,
                new SessionActivatedPayload(sessionId, new List<Guid> { Guid.NewGuid() }));

            await using var assertDb = new ScoringDbContext(opts);
            var events = await assertDb.SessionEvents
                .Where(e => e.SessionId == sessionId && e.Type == "SessionActivated")
                .ToListAsync();
            events.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public async Task HandleMessageAsync_SessionPaused_RecordsAudit()
        {
            var dbName = Guid.NewGuid().ToString();
            var opts = CreateInMemoryOptions(dbName);
            using var sp = CreateServiceProvider(opts);
            var consumer = CreateConsumer(sp);

            var sessionId = Guid.NewGuid();
            await InvokeHandleMessageAsync(consumer, SessionCreated,
                new SessionCreatedPayload(sessionId, Guid.NewGuid(), "Paused Session"));
            await InvokeHandleMessageAsync(consumer, SessionActivated,
                new SessionActivatedPayload(sessionId, new List<Guid>()));
            await InvokeHandleMessageAsync(consumer, SessionPaused,
                new SessionPausedPayload(sessionId));

            await using var assertDb = new ScoringDbContext(opts);
            var events = await assertDb.SessionEvents
                .Where(e => e.SessionId == sessionId && e.Type == "SessionPaused")
                .ToListAsync();
            events.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public async Task HandleMessageAsync_SessionFinished_RecordsAudit()
        {
            var dbName = Guid.NewGuid().ToString();
            var opts = CreateInMemoryOptions(dbName);
            using var sp = CreateServiceProvider(opts);
            var consumer = CreateConsumer(sp);

            var sessionId = Guid.NewGuid();
            await InvokeHandleMessageAsync(consumer, SessionCreated,
                new SessionCreatedPayload(sessionId, Guid.NewGuid(), "Finished Session"));
            await InvokeHandleMessageAsync(consumer, SessionActivated,
                new SessionActivatedPayload(sessionId, new List<Guid>()));
            await InvokeHandleMessageAsync(consumer, SessionFinished,
                new SessionFinishedPayload(sessionId, new List<Guid>()));

            await using var assertDb = new ScoringDbContext(opts);
            var events = await assertDb.SessionEvents
                .Where(e => e.SessionId == sessionId && e.Type == "SessionFinished")
                .ToListAsync();
            events.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public async Task HandleMessageAsync_SessionCancelled_RecordsAudit()
        {
            var dbName = Guid.NewGuid().ToString();
            var opts = CreateInMemoryOptions(dbName);
            using var sp = CreateServiceProvider(opts);
            var consumer = CreateConsumer(sp);

            var sessionId = Guid.NewGuid();
            await InvokeHandleMessageAsync(consumer, SessionCreated,
                new SessionCreatedPayload(sessionId, Guid.NewGuid(), "Cancelled Session"));
            await InvokeHandleMessageAsync(consumer, SessionActivated,
                new SessionActivatedPayload(sessionId, new List<Guid>()));
            await InvokeHandleMessageAsync(consumer, SessionCancelled,
                new SessionCancelledPayload(sessionId, new List<Guid>()));

            await using var assertDb = new ScoringDbContext(opts);
            var events = await assertDb.SessionEvents
                .Where(e => e.SessionId == sessionId && e.Type == "SessionCancelled")
                .ToListAsync();
            events.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public async Task HandleMessageAsync_StageTransitioned_RecordsAudit()
        {
            var dbName = Guid.NewGuid().ToString();
            var opts = CreateInMemoryOptions(dbName);
            using var sp = CreateServiceProvider(opts);
            var consumer = CreateConsumer(sp);

            var sessionId = Guid.NewGuid();
            await InvokeHandleMessageAsync(consumer, SessionCreated,
                new SessionCreatedPayload(sessionId, Guid.NewGuid(), "Stage Session"));
            await InvokeHandleMessageAsync(consumer, SessionActivated,
                new SessionActivatedPayload(sessionId, new List<Guid> { Guid.NewGuid() }));
            await InvokeHandleMessageAsync(consumer, StageTransitioned,
                new StageTransitionedPayload(sessionId, new List<Guid>(), new List<Guid> { Guid.NewGuid() }));

            await using var assertDb = new ScoringDbContext(opts);
            var events = await assertDb.SessionEvents
                .Where(e => e.SessionId == sessionId && e.Type == "StageTransitioned")
                .ToListAsync();
            events.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public async Task HandleMessageAsync_UnknownRoutingKey_DoesNotThrow()
        {
            var dbName = Guid.NewGuid().ToString();
            var opts = CreateInMemoryOptions(dbName);
            using var sp = CreateServiceProvider(opts);
            var consumer = CreateConsumer(sp);

            await FluentActions.Invoking(() =>
                InvokeHandleMessageAsync(consumer, "UnknownEvent", new { foo = "bar" }))
                .Should().NotThrowAsync();
        }

        [Fact]
        public void Dispose_CanBeCalledSafely()
        {
            var dbName = Guid.NewGuid().ToString();
            var opts = CreateInMemoryOptions(dbName);
            using var sp = CreateServiceProvider(opts);
            var consumer = CreateConsumer(sp);

            consumer.Invoking(c => c.Dispose()).Should().NotThrow();
        }
    }
}
