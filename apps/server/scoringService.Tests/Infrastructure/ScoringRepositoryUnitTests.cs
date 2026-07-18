using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ScoringService.Domain.Entities;
using ScoringService.Domain.Enums;
using ScoringService.Domain.ValueObjects;
using ScoringService.Infrastructure.Persistence.Contexts;
using ScoringService.Infrastructure.Persistence.Repositories;
using ScoringService.Tests;
using Xunit;

// Helper to build domain objects with the correct factory signatures

namespace ScoringService.Tests.Infrastructure
{
    public class ScoringRepositoryUnitTests
    {
        private static ScoringDbContext CreateDb()
        {
            var opts = new DbContextOptionsBuilder<ScoringDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new TestScoringDbContext(opts);
        }

        // ── EvidenceSubmission ────────────────────────────────────────────────

        [Fact]
        public async Task AddSubmissionAsync_AndGetById_ReturnsSameSubmission()
        {
            await using var db = CreateDb();
            var repo = new ScoringRepository(db);

            var sessionId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            var subStageId = Guid.NewGuid();
            var submission = EvidenceSubmission.Create(
                Guid.NewGuid(), sessionId, teamId, subStageId,
                SubmissionType.Trivia, "Prueba A", DateTime.UtcNow);

            await repo.AddSubmissionAsync(submission);
            await repo.SaveChangesAsync();

            var retrieved = await repo.GetSubmissionByIdAsync(submission.Id);
            retrieved.Should().NotBeNull();
            retrieved!.Id.Should().Be(submission.Id);
        }

        [Fact]
        public async Task GetSubmissionByIdAsync_UnknownId_ReturnsNull()
        {
            await using var db = CreateDb();
            var repo = new ScoringRepository(db);

            var result = await repo.GetSubmissionByIdAsync(Guid.NewGuid());
            result.Should().BeNull();
        }

        [Fact]
        public async Task HasSolvedSubStageAsync_WhenCorrectSubmissionExists_ReturnsTrue()
        {
            await using var db = CreateDb();
            var repo = new ScoringRepository(db);

            var sessionId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            var subStageId = Guid.NewGuid();

            var submission = EvidenceSubmission.Create(
                Guid.NewGuid(), sessionId, teamId, subStageId,
                SubmissionType.Trivia, "OK", DateTime.UtcNow);
            submission.MarkCorrect(10, DateTime.UtcNow);
            await repo.AddSubmissionAsync(submission);
            await repo.SaveChangesAsync();

            var result = await repo.HasSolvedSubStageAsync(sessionId, teamId, subStageId);
            result.Should().BeTrue();
        }

        [Fact]
        public async Task HasSolvedSubStageAsync_WhenNoCorrectSubmission_ReturnsFalse()
        {
            await using var db = CreateDb();
            var repo = new ScoringRepository(db);

            var result = await repo.HasSolvedSubStageAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            result.Should().BeFalse();
        }

        [Fact]
        public async Task HasAnySubmissionAsync_WhenSubmissionExists_ReturnsTrue()
        {
            await using var db = CreateDb();
            var repo = new ScoringRepository(db);

            var sessionId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            var subStageId = Guid.NewGuid();

            var submission = EvidenceSubmission.Create(
                Guid.NewGuid(), sessionId, teamId, subStageId,
                SubmissionType.Trivia, "Any", DateTime.UtcNow);
            await repo.AddSubmissionAsync(submission);
            await repo.SaveChangesAsync();

            var result = await repo.HasAnySubmissionAsync(sessionId, teamId, subStageId);
            result.Should().BeTrue();
        }

        [Fact]
        public async Task HasAnySubmissionAsync_WhenNoSubmission_ReturnsFalse()
        {
            await using var db = CreateDb();
            var repo = new ScoringRepository(db);

            var result = await repo.HasAnySubmissionAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetSubmissionsForSubStagesAsync_WithEmptyIds_ReturnsEmpty()
        {
            await using var db = CreateDb();
            var repo = new ScoringRepository(db);

            var result = await repo.GetSubmissionsForSubStagesAsync(Guid.NewGuid(), new List<Guid>());
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetSubmissionsForSubStagesAsync_WithMatchingIds_ReturnsData()
        {
            await using var db = CreateDb();
            var repo = new ScoringRepository(db);

            var sessionId = Guid.NewGuid();
            var subStageId = Guid.NewGuid();
            var submission = EvidenceSubmission.Create(
                Guid.NewGuid(), sessionId, Guid.NewGuid(), subStageId,
                SubmissionType.Trivia, "X", DateTime.UtcNow);
            await repo.AddSubmissionAsync(submission);
            await repo.SaveChangesAsync();

            var result = await repo.GetSubmissionsForSubStagesAsync(sessionId, new List<Guid> { subStageId });
            result.Should().HaveCount(1);
        }

        // ── ScoreEntry ────────────────────────────────────────────────────────

        [Fact]
        public async Task AddScoreEntryAsync_AndGetBySession_ReturnsEntry()
        {
            await using var db = CreateDb();
            var repo = new ScoringRepository(db);

            var sessionId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            var entry = ScoreEntry.ForEvidence(
                Guid.NewGuid(), sessionId, teamId,
                new ScoreValue(100), Guid.NewGuid(), DateTime.UtcNow);

            await repo.AddScoreEntryAsync(entry);
            await repo.SaveChangesAsync();

            var entries = await repo.GetScoreEntriesBySessionAsync(sessionId);
            entries.Should().HaveCount(1);
            entries[0].Value.Points.Should().Be(100);
        }

        [Fact]
        public async Task GetScoreEntriesByTeamAsync_ReturnsOnlyTeamEntries()
        {
            await using var db = CreateDb();
            var repo = new ScoringRepository(db);

            var sessionId = Guid.NewGuid();
            var teamA = Guid.NewGuid();
            var teamB = Guid.NewGuid();

            await repo.AddScoreEntryAsync(ScoreEntry.ForEvidence(Guid.NewGuid(), sessionId, teamA, new ScoreValue(50), Guid.NewGuid(), DateTime.UtcNow));
            await repo.AddScoreEntryAsync(ScoreEntry.ForEvidence(Guid.NewGuid(), sessionId, teamB, new ScoreValue(75), Guid.NewGuid(), DateTime.UtcNow));
            await repo.SaveChangesAsync();

            var result = await repo.GetScoreEntriesByTeamAsync(sessionId, teamA);
            result.Should().HaveCount(1);
            result[0].TeamId.Should().Be(teamA);
        }

        // ── SessionEvent ──────────────────────────────────────────────────────

        [Fact]
        public async Task AddSessionEventAsync_AndGetBySession_ReturnsOrderedEvents()
        {
            await using var db = CreateDb();
            var repo = new ScoringRepository(db);

            var sessionId = Guid.NewGuid();
            var earlier = DateTime.UtcNow.AddMinutes(-5);
            var later = DateTime.UtcNow;

            await repo.AddSessionEventAsync(SessionEvent.Record(Guid.NewGuid(), sessionId, "TypeA", "Primero", earlier));
            await repo.AddSessionEventAsync(SessionEvent.Record(Guid.NewGuid(), sessionId, "TypeB", "Segundo", later));
            await repo.SaveChangesAsync();

            var events = await repo.GetSessionEventsAsync(sessionId);
            events.Should().HaveCount(2);
            // GetSessionEventsAsync ordena por OccurredAt descending
            events[0].OccurredAt.Should().BeOnOrAfter(events[1].OccurredAt);
        }
    }
}
