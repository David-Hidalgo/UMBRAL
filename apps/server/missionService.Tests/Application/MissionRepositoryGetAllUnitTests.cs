using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MissionService.Domain.Entities;
using MissionService.Domain.Enums;
using MissionService.Infrastructure.Persistence;
using Xunit;

namespace MissionService.Tests.Application
{
    /// <summary>
    /// Unit tests for MissionRepository.GetAllAsync using the EF Core InMemory provider as a fake.
    /// These tests run without Docker and are fast. They cover structural behavior and LINQ filtering logic.
    /// </summary>
    public class MissionRepositoryGetAllUnitTests : IDisposable
    {
        private readonly MissionDbContext _context;
        private readonly MissionRepository _repository;

        public MissionRepositoryGetAllUnitTests()
        {
            var options = new DbContextOptionsBuilder<MissionDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new MissionDbContext(options);
            _repository = new MissionRepository(_context);
        }

        public void Dispose() => _context.Dispose();

        // ─── Empty state ──────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_Should_Return_Empty_Collection_When_No_Missions_Exist()
        {
            // Arrange — empty in-memory database

            // Act
            var result = await _repository.GetAllAsync(null, null);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        // ─── No filters ───────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_Should_Return_All_Missions_When_Both_Filters_Are_Null()
        {
            // Arrange
            await SeedMissions(
                CreateMission("Alpha", "A"),
                CreateMission("Beta", "B"),
                CreateMission("Gamma", "C"));

            // Act
            var result = await _repository.GetAllAsync(null, null);

            // Assert
            result.Should().HaveCount(3);
        }

        [Fact]
        public async Task GetAllAsync_Should_Return_Missions_Sorted_Alphabetically_By_Name()
        {
            // Arrange — seeded in reverse alphabetical order
            await SeedMissions(
                CreateMission("Zebra Mission", "Z"),
                CreateMission("Alpha Mission", "A"),
                CreateMission("Mango Mission", "M"));

            // Act
            var result = await _repository.GetAllAsync(null, null);

            // Assert
            result.Select(m => m.Name).Should().BeInAscendingOrder();
        }

        // ─── SearchTerm filter ────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_Should_Return_Only_Missions_Whose_Name_Contains_SearchTerm()
        {
            // Arrange
            await SeedMissions(
                CreateMission("Alpha Quest", "A"),
                CreateMission("Beta Expedition", "B"),
                CreateMission("Alpha Rescue", "C"));

            // Act
            var result = await _repository.GetAllAsync("alpha", null);

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(m => m.Name.ToLower().Should().Contain("alpha"));
        }

        [Fact]
        public async Task GetAllAsync_Should_Match_SearchTerm_Case_Insensitively()
        {
            // Arrange
            await SeedMissions(CreateMission("Treasure Island", "T"));

            // Act
            var lower = await _repository.GetAllAsync("treasure", null);
            var upper = await _repository.GetAllAsync("TREASURE", null);
            var mixed = await _repository.GetAllAsync("Treasure", null);

            // Assert
            lower.Should().HaveCount(1);
            upper.Should().HaveCount(1);
            mixed.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetAllAsync_Should_Return_Empty_When_SearchTerm_Matches_Nothing()
        {
            // Arrange
            await SeedMissions(CreateMission("Alpha Mission", "A"));

            // Act
            var result = await _repository.GetAllAsync("zzz", null);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllAsync_Should_Treat_Whitespace_Only_SearchTerm_As_No_Filter()
        {
            // Arrange
            await SeedMissions(
                CreateMission("Alpha", "A"),
                CreateMission("Beta", "B"));

            // Act — whitespace guard skips the filter, returning all
            var result = await _repository.GetAllAsync("   ", null);

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetAllAsync_Should_Match_SearchTerm_As_Partial_Substring()
        {
            // Arrange
            await SeedMissions(
                CreateMission("Great Adventure", "G"),
                CreateMission("Grand Canyon Mission", "C"),
                CreateMission("Ordinary Mission", "O"));

            // Act — "gr" should match "Great" and "Grand" but not "Ordinary"
            var result = await _repository.GetAllAsync("gr", null);

            // Assert
            result.Should().HaveCount(2);
        }

        // ─── Status filter (basic) ──

        [Fact]
        public async Task GetAllAsync_Should_Skip_Status_Filter_When_Status_Is_Null()
        {
            // Arrange
            await SeedMissions(
                CreateMission("Mission A", "A"),
                CreateMission("Mission B", "B"));

            // Act
            var result = await _repository.GetAllAsync(null, null);

            // Assert — returns all regardless of status
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetAllAsync_Should_Throw_ArgumentException_When_Status_Is_Whitespace()
        {
            // Arrange
            await SeedMissions(CreateMission("Mission A", "A"));

            // Act
            Func<Task> act = () => _repository.GetAllAsync(null, "   ");

            // Assert — whitespace is an invalid status value and should throw rather than being ignored
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Status value cannot be empty.*");
        }

        // ─── Combined filters ─────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_Should_Apply_SearchTerm_When_Combined_With_Null_Status()
        {
            // Arrange
            await SeedMissions(
                CreateMission("Alpha One", "A"),
                CreateMission("Beta Two", "B"),
                CreateMission("Alpha Three", "C"));

            // Act
            var result = await _repository.GetAllAsync("alpha", null);

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(m => m.Name.ToLower().Should().Contain("alpha"));
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private async Task SeedMissions(params Mission[] missions)
        {
            foreach (var m in missions)
                _context.Missions.Add(m);
            await _context.SaveChangesAsync();
        }

        private static Mission CreateMission(string name, string description)
            => Mission.Create(Guid.NewGuid(), name, description, DifficultyLevel.Beginner, 60, new List<Stage>());
    }
}
