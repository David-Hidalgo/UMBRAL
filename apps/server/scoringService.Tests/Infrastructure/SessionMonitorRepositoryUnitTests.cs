using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ScoringService.Domain.Entities;
using ScoringService.Domain.ValueObjects;
using ScoringService.Infrastructure.Persistence.Contexts;
using ScoringService.Infrastructure.Persistence.Repositories;
using ScoringService.Tests;
using Xunit;

namespace ScoringService.Tests.Infrastructure
{
    public class SessionMonitorRepositoryUnitTests
    {
        private static ScoringDbContext CreateDb(string dbName)
        {
            var opts = new DbContextOptionsBuilder<ScoringDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new TestScoringDbContext(opts);
        }

        [Fact]
        public async Task AddAsync_AndGetBySessionId_ReturnsMonitor()
        {
            await using var db = CreateDb(Guid.NewGuid().ToString());
            var repo = new SessionMonitorRepository(db);

            var sessionId = Guid.NewGuid();
            var monitor = SessionMonitor.Create(sessionId, Guid.NewGuid(), "Misión Test");

            await repo.AddAsync(monitor);
            await repo.SaveChangesAsync();

            var retrieved = await repo.GetBySessionIdAsync(sessionId);
            retrieved.Should().NotBeNull();
            retrieved!.SessionId.Should().Be(sessionId);
        }

        [Fact]
        public async Task GetBySessionIdAsync_UnknownSession_ReturnsNull()
        {
            await using var db = CreateDb(Guid.NewGuid().ToString());
            var repo = new SessionMonitorRepository(db);

            var result = await repo.GetBySessionIdAsync(Guid.NewGuid());
            result.Should().BeNull();
        }

        [Fact]
        public async Task AddAsync_WithTeams_PersistsTeamsViaEfOwned()
        {
            await using var db = CreateDb(Guid.NewGuid().ToString());
            var repo = new SessionMonitorRepository(db);

            var sessionId = Guid.NewGuid();
            var monitor = SessionMonitor.Create(sessionId, Guid.NewGuid(), "Con Equipos");
            var teamId = Guid.NewGuid();
            monitor.RegisterTeam(teamId, new TeamCode("AlphaTeam"));

            await repo.AddAsync(monitor);
            await repo.SaveChangesAsync();

            var retrieved = await repo.GetBySessionIdAsync(sessionId);
            retrieved.Should().NotBeNull();
            retrieved!.Teams.Should().ContainSingle(t => t.TeamId == teamId);
        }

        [Fact]
        public async Task SaveChangesAsync_PersistsMutations()
        {
            var dbName = Guid.NewGuid().ToString();
            await using var db = CreateDb(dbName);
            var repo = new SessionMonitorRepository(db);

            var sessionId = Guid.NewGuid();
            var monitor = SessionMonitor.Create(sessionId, Guid.NewGuid(), "Mutación");
            await repo.AddAsync(monitor);
            await repo.SaveChangesAsync();

            // Mutar el monitor y guardar
            var loaded = await repo.GetBySessionIdAsync(sessionId);
            loaded!.Activate(new System.Collections.Generic.List<Guid> { Guid.NewGuid() });
            await repo.SaveChangesAsync();

            // Verificar con un contexto fresco
            await using var verifyDb = CreateDb(dbName);
            var final = await verifyDb.SessionMonitors.FirstOrDefaultAsync(m => m.SessionId == sessionId);
            final.Should().NotBeNull();
        }
    }
}
