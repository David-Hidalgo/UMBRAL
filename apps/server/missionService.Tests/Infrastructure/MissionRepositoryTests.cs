using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MissionService.Domain.Entities;
using MissionService.Domain.Enums;
using MissionService.Infrastructure.Persistence;
using Xunit;

namespace MissionService.Tests.Infrastructure
{
    public class MissionRepositoryTests : IDisposable
    {
        private readonly MissionDbContext _context;
        private readonly MissionRepository _repository;

        public MissionRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<MissionDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new MissionDbContext(options);
            _repository = new MissionRepository(_context);
        }

        public void Dispose() => _context.Dispose();

        // ─── ExistsByNameAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task ExistsByNameAsync_Should_Return_False_When_Name_Is_Null()
        {
            // Arrange
            await SeedMission("Existing Mission");

            // Act
            var result = await _repository.ExistsByNameAsync(null!);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExistsByNameAsync_Should_Return_False_When_Name_Is_WhiteSpace()
        {
            // Arrange
            await SeedMission("Existing Mission");

            // Act
            var result = await _repository.ExistsByNameAsync("   ");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExistsByNameAsync_Should_Return_True_When_Mission_With_Same_Name_Exists()
        {
            // Arrange
            await SeedMission("Treasure Hunt");

            // Act
            var result = await _repository.ExistsByNameAsync("Treasure Hunt");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByNameAsync_Should_Return_False_When_No_Mission_With_That_Name_Exists()
        {
            // Arrange
            await SeedMission("Other Mission");

            // Act
            var result = await _repository.ExistsByNameAsync("Treasure Hunt");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExistsByNameAsync_Should_Match_Name_Case_Insensitively()
        {
            // Arrange
            await SeedMission("Treasure Hunt");

            // Act
            var resultLower = await _repository.ExistsByNameAsync("treasure hunt");
            var resultUpper = await _repository.ExistsByNameAsync("TREASURE HUNT");

            // Assert
            resultLower.Should().BeTrue();
            resultUpper.Should().BeTrue();
        }

        // ─── ExistsByNameExcludingIdAsync ─────────────────────────────────────────

        [Fact]
        public async Task ExistsByNameExcludingIdAsync_Should_Return_False_When_Name_Is_Null()
        {
            // Arrange
            await SeedMission("Existing Mission");

            // Act
            var result = await _repository.ExistsByNameExcludingIdAsync(null!, Guid.NewGuid());

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExistsByNameExcludingIdAsync_Should_Return_False_When_Name_Is_WhiteSpace()
        {
            // Arrange
            await SeedMission("Existing Mission");

            // Act
            var result = await _repository.ExistsByNameExcludingIdAsync("   ", Guid.NewGuid());

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExistsByNameExcludingIdAsync_Should_Return_False_When_Only_Same_Mission_Has_That_Name()
        {
            // Arrange — la misión que queremos actualizar ya tiene ese nombre: no es duplicado
            var missionId = Guid.NewGuid();
            await SeedMission("Treasure Hunt", missionId);

            // Act
            var result = await _repository.ExistsByNameExcludingIdAsync("Treasure Hunt", missionId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExistsByNameExcludingIdAsync_Should_Return_True_When_Different_Mission_Has_Same_Name()
        {
            // Arrange — otra misión distinta ya ocupa el nombre
            await SeedMission("Treasure Hunt");
            var editingId = Guid.NewGuid();
            await SeedMission("Different Mission", editingId);

            // Act
            var result = await _repository.ExistsByNameExcludingIdAsync("Treasure Hunt", editingId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByNameExcludingIdAsync_Should_Match_Name_Case_Insensitively()
        {
            // Arrange
            await SeedMission("Treasure Hunt");
            var editingId = Guid.NewGuid();
            await SeedMission("Other Mission", editingId);

            // Act
            var resultLower = await _repository.ExistsByNameExcludingIdAsync("treasure hunt", editingId);
            var resultUpper = await _repository.ExistsByNameExcludingIdAsync("TREASURE HUNT", editingId);

            // Assert
            resultLower.Should().BeTrue();
            resultUpper.Should().BeTrue();
        }

        // ─── AddAsync ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task AddAsync_Should_Persist_Mission_In_Context()
        {
            // Arrange
            var mission = CreateMission("New Mission");

            // Act
            await _repository.AddAsync(mission);
            await _repository.SaveChangesAsync();

            // Assert
            var stored = await _context.Missions.FindAsync(mission.Id);
            stored.Should().NotBeNull();
            stored!.Name.Should().Be("New Mission");
        }

        // ─── Remove ───────────────────────────────────────────────────────────────

        [Fact]
        public async Task Remove_Should_Delete_Mission_From_Context()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            await SeedMission("Mission To Delete", missionId);
            var mission = await _context.Missions.FindAsync(missionId);

            // Act
            _repository.Remove(mission!);
            await _repository.SaveChangesAsync();

            // Assert
            var deleted = await _context.Missions.FindAsync(missionId);
            deleted.Should().BeNull();
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private async Task SeedMission(string name, Guid? id = null)
        {
            var mission = CreateMission(name, id);
            _context.Missions.Add(mission);
            await _context.SaveChangesAsync();
        }

        private static Mission CreateMission(string name, Guid? id = null)
            => Mission.Create(id ?? Guid.NewGuid(), name, "Desc", DifficultyLevel.Beginner, 60, new List<Stage>());
    }
}
