using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Infrastructure.Persistence;
using LiveSessionService.Tests.Support;

namespace LiveSessionService.Tests.Infrastructure
{
    public class LiveSessionRepositoryTests : IDisposable
    {
        private readonly LiveSessionDbContext _context;
        private readonly LiveSessionRepository _repository;

        public LiveSessionRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<LiveSessionDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new LiveSessionDbContext(options);
            _repository = new LiveSessionRepository(_context);
        }

        public void Dispose() => _context.Dispose();

        [Fact]
        public async Task GetByIdAsync_Should_Return_Null_When_Session_Does_Not_Exist()
        {
            // Act
            var result = await _repository.GetByIdAsync(Guid.NewGuid());

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task AddAsync_And_GetByIdAsync_Should_Persist_Session()
        {
            // Arrange
            var session = CreateSession("Partida 1");

            // Act
            await _repository.AddAsync(session);
            await _repository.SaveChangesAsync();
            var result = await _repository.GetByIdAsync(session.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("Partida 1");
        }

        [Fact]
        public async Task AddTeamAsync_Should_Persist_Team_With_Players()
        {
            // Arrange
            var session = CreateSession("Partida 1");
            await _repository.AddAsync(session);
            await _repository.SaveChangesAsync();

            var team = Team.Create(Guid.NewGuid(), "Alfa", session.Id);
            var player = Player.Create(Guid.NewGuid(), "user-1", "Ana", team.Id);
            team.AddPlayer(player);

            // Act
            await _repository.AddTeamAsync(team);
            await _repository.SaveChangesAsync();
            var result = await _repository.GetByIdAsync(session.Id);

            // Assert
            result!.Teams.Should().ContainSingle(t => t.Name == "Alfa" && t.Players.Count == 1);
        }

        [Fact]
        public async Task AddPlayerAsync_Should_Persist_Player()
        {
            // Arrange
            var session = CreateSession("Partida 1");
            var team = Team.Create(Guid.NewGuid(), "Alfa", session.Id);
            session.RegisterTeam(team);

            await _repository.AddAsync(session);
            await _repository.AddTeamAsync(team);
            await _repository.SaveChangesAsync();

            var player = Player.Create(Guid.NewGuid(), "user-1", "Ana", team.Id);

            // Act
            await _repository.AddPlayerAsync(player);
            await _repository.SaveChangesAsync();
            var result = await _repository.GetByIdAsync(session.Id);

            // Assert
            result!.Teams.Single().Players.Should().ContainSingle(p => p.KeycloakUserId == "user-1");
        }

        [Fact]
        public async Task GetAllAsync_Should_Return_Empty_When_No_Sessions_Exist()
        {
            // Act
            var result = await _repository.GetAllAsync(null, null);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllAsync_Should_Filter_By_MissionId()
        {
            // Arrange
            var missionA = Guid.NewGuid();
            var missionB = Guid.NewGuid();
            var sessionA = CreateSession("A", missionA);
            var sessionB = CreateSession("B", missionB);

            await _repository.AddAsync(sessionA);
            await _repository.AddAsync(sessionB);
            await _repository.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllAsync(missionA, null);

            // Assert
            result.Should().ContainSingle(s => s.MissionId == missionA);
        }

        [Fact]
        public async Task GetAllAsync_Should_Filter_By_Status()
        {
            // Arrange
            var scheduled = CreateSession("Programada");
            var active = CreateSession("Activa");
            active.StartPreparation();
            active.RegisterTeamWithPlayer();
            active.Activate();

            await _repository.AddAsync(scheduled);
            await _repository.AddAsync(active);
            await _repository.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllAsync(null, LiveSessionStatus.Activa.ToString());

            // Assert
            result.Should().ContainSingle(s => s.Status == LiveSessionStatus.Activa);
        }

        [Fact]
        public async Task GetAllAsync_Should_Ignore_Invalid_Status_Filter()
        {
            // Arrange
            var session = CreateSession("Partida");
            await _repository.AddAsync(session);
            await _repository.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllAsync(null, "EstadoInvalido");

            // Assert
            result.Should().HaveCount(1);
        }

        private static LiveSession CreateSession(string name, Guid? missionId = null)
        {
            return LiveSession.Create(
                Guid.NewGuid(),
                missionId ?? Guid.NewGuid(),
                "Misión",
                name,
                null);
        }
    }
}
