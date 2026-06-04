using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MissionService.Domain.Entities;
using MissionService.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace MissionService.Tests.Application
{
    /// <summary>
    /// Integration tests for MissionRepository.GetAllAsync using a real PostgreSQL
    /// instance via Testcontainers. Validates filtering logic against the actual DB engine,
    /// including EF Core value conversions for MissionStatus.
    /// </summary>
    [Trait("Category", "Integration")]
    public class MissionRepositoryGetAllTests : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();

        private MissionDbContext _context = null!;
        private MissionRepository _repository = null!;

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();

            var options = new DbContextOptionsBuilder<MissionDbContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options;

            _context = new MissionDbContext(options);
            await _context.Database.MigrateAsync();
            _repository = new MissionRepository(_context);
        }

        public async Task DisposeAsync()
        {
            await _context.DisposeAsync();
            await _postgres.DisposeAsync();
        }

        // ─── Tests ────────────────────────────────────────────────────────────────

        [IntegrationFact]
        public async Task GetAllAsync_Should_Return_Empty_When_No_Missions_Exist()
        {
            // Arrange — empty database

            // Act
            var result = await _repository.GetAllAsync(null, null);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [IntegrationFact]
        public async Task GetAllAsync_Should_Return_All_Missions_When_No_Filters_Applied()
        {
            // Arrange
            await SeedMissions(
                CreateMission("Alpha Mission", "Alpha description"),
                CreateMission("Beta Mission", "Beta description"),
                CreateMission("Gamma Mission", "Gamma description"));

            // Act
            var result = await _repository.GetAllAsync(null, null);

            // Assert
            result.Should().HaveCount(3);
        }

        [IntegrationFact]
        public async Task GetAllAsync_Should_Return_Missions_Ordered_By_Name()
        {
            // Arrange
            await SeedMissions(
                CreateMission("Zebra Mission", "Z"),
                CreateMission("Alpha Mission", "A"),
                CreateMission("Mango Mission", "M"));

            // Act
            var result = await _repository.GetAllAsync(null, null);

            // Assert
            var names = result.Select(m => m.Name).ToList();
            names.Should().BeInAscendingOrder();
        }

        [IntegrationFact]
        public async Task GetAllAsync_Should_Filter_By_Name_When_SearchTerm_Is_Provided()
        {
            // Arrange
            await SeedMissions(
                CreateMission("Alpha Mission", "A"),
                CreateMission("Beta Expedition", "B"),
                CreateMission("Alpha Quest", "C"));

            // Act
            var result = await _repository.GetAllAsync("alpha", null);

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(m => m.Name.ToLower().Should().Contain("alpha"));
        }

        [IntegrationFact]
        public async Task GetAllAsync_Should_Filter_Case_Insensitively_By_SearchTerm()
        {
            // Arrange
            await SeedMissions(CreateMission("Treasure Hunt Adventure", "T"));

            // Act — different casings
            var resultLower = await _repository.GetAllAsync("treasure", null);
            var resultUpper = await _repository.GetAllAsync("TREASURE", null);
            var resultMixed = await _repository.GetAllAsync("Treasure", null);

            // Assert
            resultLower.Should().HaveCount(1);
            resultUpper.Should().HaveCount(1);
            resultMixed.Should().HaveCount(1);
        }

        [IntegrationFact]
        public async Task GetAllAsync_Should_Filter_By_Status_Active_When_Status_Is_Provided()
        {
            // Arrange
            var activeMission = CreateMission("Active Mission", "A");
            activeMission.Activate();
            var inactiveMission = CreateMission("Inactive Mission", "I");

            await SeedMissions(activeMission, inactiveMission);

            // Act
            var activeResults = await _repository.GetAllAsync(null, "Active");

            // Assert
            activeResults.Should().HaveCount(1);
            activeResults.Single().Name.Should().Be("Active Mission");
        }

        [IntegrationFact]
        public async Task GetAllAsync_Should_Filter_By_Status_Inactive_When_Status_Is_Provided()
        {
            // Arrange
            var activeMission = CreateMission("Active Mission", "A");
            activeMission.Activate();
            var inactiveMission = CreateMission("Inactive Mission", "I");

            await SeedMissions(activeMission, inactiveMission);

            // Act
            var inactiveResults = await _repository.GetAllAsync(null, "Inactive");

            // Assert
            inactiveResults.Should().HaveCount(1);
            inactiveResults.Single().Name.Should().Be("Inactive Mission");
        }

        [IntegrationFact]
        public async Task GetAllAsync_Should_Return_Empty_When_Status_Filter_Matches_Nothing()
        {
            // Arrange — all missions start as Inactive
            await SeedMissions(CreateMission("Inactive Only Mission", "I"));

            // Act
            var result = await _repository.GetAllAsync(null, "Active");

            // Assert
            result.Should().BeEmpty();
        }

        [IntegrationFact]
        public async Task GetAllAsync_Should_Apply_Both_Filters_Together()
        {
            // Arrange
            var activeMission1 = CreateMission("Alpha Active", "AA");
            activeMission1.Activate();

            var activeMission2 = CreateMission("Beta Active", "BA");
            activeMission2.Activate();

            var inactiveMission = CreateMission("Alpha Inactive", "AI");

            await SeedMissions(activeMission1, activeMission2, inactiveMission);

            // Act — match "alpha" AND "Active"
            var result = await _repository.GetAllAsync("alpha", "Active");

            // Assert
            result.Should().HaveCount(1);
            result.Single().Name.Should().Be("Alpha Active");
        }

        [IntegrationFact]
        public async Task GetAllAsync_Should_Ignore_Whitespace_Only_SearchTerm()
        {
            // Arrange
            await SeedMissions(CreateMission("Any Mission", "Any"));

            // Act — whitespace guard in repository skips the filter
            var result = await _repository.GetAllAsync("   ", null);

            // Assert — returns all missions
            result.Should().HaveCount(1);
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private async Task SeedMissions(params Mission[] missions)
        {
            foreach (var mission in missions)
                _context.Missions.Add(mission);

            await _context.SaveChangesAsync();
        }

        private static Mission CreateMission(string name, string description)
        {
            return Mission.Create(Guid.NewGuid(), name, description, new List<Stage>());
        }
    }

    // ─── Dynamic Skip & Custom Attribute Implementations ──────────────────────────

    internal static class DockerHelper
    {
        private static readonly Lazy<bool> _isDockerAvailable = new(() =>
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "info",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(startInfo);
                if (process == null) return false;
                process.WaitForExit(1000); // 1-second timeout
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        });

        public static bool IsDockerAvailable => _isDockerAvailable.Value;
    }

    public sealed class IntegrationFactAttribute : FactAttribute
    {
        public IntegrationFactAttribute()
        {
            var runIntegration = Environment.GetEnvironmentVariable("RUN_INTEGRATION_TESTS");
            if (runIntegration != "true")
            {
                Skip = "Integration tests are disabled for the current milestone. Set RUN_INTEGRATION_TESTS=true to run them.";
                return;
            }

            if (!DockerHelper.IsDockerAvailable)
            {
                Skip = "Docker is not running/available. This integration test requires Docker.";
            }
        }
    }

    public sealed class IntegrationTheoryAttribute : TheoryAttribute
    {
        public IntegrationTheoryAttribute()
        {
            var runIntegration = Environment.GetEnvironmentVariable("RUN_INTEGRATION_TESTS");
            if (runIntegration != "true")
            {
                Skip = "Integration tests are disabled for the current milestone. Set RUN_INTEGRATION_TESTS=true to run them.";
                return;
            }

            if (!DockerHelper.IsDockerAvailable)
            {
                Skip = "Docker is not running/available. This integration test requires Docker.";
            }
        }
    }
}