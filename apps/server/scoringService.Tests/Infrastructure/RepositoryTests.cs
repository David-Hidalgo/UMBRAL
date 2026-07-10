using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ScoringService.Domain.Entities;
using ScoringService.Domain.Enums;
using ScoringService.Domain.ValueObjects;
using ScoringService.Infrastructure.Persistence.Contexts;
using ScoringService.Infrastructure.Persistence.Repositories;
using Testcontainers.PostgreSql;
using Xunit;

namespace ScoringService.Tests.Infrastructure
{
    /// <summary>
    /// Pruebas de integración de los repositorios contra un PostgreSQL real (Testcontainers,
    /// quality-spec §1). Validan el mapeo genuino de los value objects (ComplexProperty de
    /// ScoreValue → columna Points, converters de PenaltyReason y TeamCode) y la colección
    /// primitiva uuid[], que el proveedor InMemory no puede materializar.
    /// </summary>
    [Trait("Category", "Integration")]
    public sealed class RepositoryTests : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();

        private DbContextOptions<ScoringDbContext> _options = null!;

        public async Task InitializeAsync()
        {
            if (Environment.GetEnvironmentVariable("RUN_INTEGRATION_TESTS") != "true" || !DockerHelper.IsDockerAvailable)
                return;

            await _postgres.StartAsync();
            _options = new DbContextOptionsBuilder<ScoringDbContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options;

            await using var context = new ScoringDbContext(_options);
            await context.Database.MigrateAsync();
        }

        public async Task DisposeAsync()
        {
            if (_options is not null)
                await _postgres.DisposeAsync();
        }

        private ScoringDbContext NewContext() => new(_options);

        [IntegrationFact]
        public async Task ScoringRepository_ShouldPersistAndQuerySubmissionsAndEntries()
        {
            var sessionId = Guid.NewGuid();
            var teamId = Guid.NewGuid();

            await using (var context = NewContext())
            {
                var repo = new ScoringRepository(context);
                var submission = EvidenceSubmission.Create(
                    Guid.NewGuid(), sessionId, teamId, Guid.NewGuid(), SubmissionType.Trivia, "resp", DateTime.UtcNow);
                submission.MarkCorrect(100, DateTime.UtcNow);
                await repo.AddSubmissionAsync(submission);
                await repo.AddScoreEntryAsync(ScoreEntry.ForEvidence(
                    Guid.NewGuid(), sessionId, teamId, ScoreValue.Award(100), submission.Id, DateTime.UtcNow));
                await repo.AddSessionEventAsync(SessionEvent.Record(
                    Guid.NewGuid(), sessionId, "EvidenceSubmitted", "desc", DateTime.UtcNow));
                await repo.SaveChangesAsync();
            }

            await using (var context = NewContext())
            {
                var repo = new ScoringRepository(context);

                var entries = await repo.GetScoreEntriesByTeamAsync(sessionId, teamId);
                entries.Should().ContainSingle();
                entries[0].Value.Points.Should().Be(100); // ComplexProperty round-trip

                var events = await repo.GetSessionEventsAsync(sessionId);
                events.Should().ContainSingle();
            }
        }

        [IntegrationFact]
        public async Task ScoringRepository_ShouldPersistPenaltyReasonValueObject()
        {
            var sessionId = Guid.NewGuid();
            var teamId = Guid.NewGuid();

            await using (var context = NewContext())
            {
                var repo = new ScoringRepository(context);
                await repo.AddScoreEntryAsync(ScoreEntry.ForPenalty(
                    Guid.NewGuid(), sessionId, teamId, ScoreValue.Penalty(25), new PenaltyReason("Uso indebido"), DateTime.UtcNow));
                await repo.SaveChangesAsync();
            }

            await using (var context = NewContext())
            {
                var repo = new ScoringRepository(context);
                var entries = await repo.GetScoreEntriesBySessionAsync(sessionId);

                entries.Should().ContainSingle();
                entries[0].Value.Points.Should().Be(-25);
                entries[0].Reason!.Value.Should().Be("Uso indebido"); // converter round-trip
            }
        }

        [IntegrationFact]
        public async Task SessionMonitorRepository_ShouldRoundTripTeamsAndSubStages()
        {
            var sessionId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            var subStageId = Guid.NewGuid();

            await using (var context = NewContext())
            {
                var repo = new SessionMonitorRepository(context);
                var monitor = SessionMonitor.Create(sessionId, Guid.NewGuid(), "Sesión");
                monitor.RegisterTeam(teamId, new TeamCode("Alfa"));
                monitor.Activate(new List<Guid> { subStageId });
                await repo.AddAsync(monitor);
                await repo.SaveChangesAsync();
            }

            await using (var context = NewContext())
            {
                var repo = new SessionMonitorRepository(context);
                var reloaded = await repo.GetBySessionIdAsync(sessionId);

                reloaded.Should().NotBeNull();
                reloaded!.Status.Should().Be(SessionMonitorStatus.Active);
                reloaded.HasTeam(teamId).Should().BeTrue();
                reloaded.Teams.Should().ContainSingle(t => t.Code.Value == "Alfa"); // TeamCode converter
                reloaded.ActiveSubStageIds.Should().ContainSingle().Which.Should().Be(subStageId); // uuid[] round-trip
            }
        }

        [IntegrationFact]
        public async Task SessionMonitorRepository_ShouldRegisterTeamOnAnExistingMonitor()
        {
            // Reproduce el flujo del consumidor: el monitor se crea en una transacción (SessionCreated)
            // y el equipo se agrega en OTRA transacción posterior (TeamRegistered). Con una clave
            // natural compuesta EF emitía UPDATE (0 filas) en vez de INSERT.
            var sessionId = Guid.NewGuid();
            var teamId = Guid.NewGuid();

            await using (var context = NewContext())
            {
                var repo = new SessionMonitorRepository(context);
                await repo.AddAsync(SessionMonitor.Create(sessionId, Guid.NewGuid(), "Sesión"));
                await repo.SaveChangesAsync();
            }

            await using (var context = NewContext())
            {
                var repo = new SessionMonitorRepository(context);
                var monitor = await repo.GetBySessionIdAsync(sessionId);
                monitor!.RegisterTeam(teamId, new TeamCode("Bravo"));
                await repo.SaveChangesAsync(); // antes lanzaba DbUpdateConcurrencyException
            }

            await using (var context = NewContext())
            {
                var repo = new SessionMonitorRepository(context);
                var reloaded = await repo.GetBySessionIdAsync(sessionId);
                reloaded!.HasTeam(teamId).Should().BeTrue();
                reloaded.Teams.Should().ContainSingle(t => t.Code.Value == "Bravo");
            }
        }
    }
}
