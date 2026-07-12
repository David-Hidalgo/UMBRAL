using System;
using FluentAssertions;
using ScoringService.Domain.Entities;
using ScoringService.Domain.Enums;
using ScoringService.Domain.ValueObjects;
using Xunit;

namespace ScoringService.Tests.Domain
{
    public class ScoreEntryTests
    {
        [Fact]
        public void ForEvidence_ShouldTraceToSubmission()
        {
            var submissionId = Guid.NewGuid();

            var entry = ScoreEntry.ForEvidence(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                ScoreValue.Award(100), submissionId, DateTime.UtcNow);

            entry.Origin.Should().Be(ScoreOrigin.EvidenceValidated);
            entry.SourceSubmissionId.Should().Be(submissionId);
            entry.Value.Points.Should().Be(100);
            entry.Reason.Should().BeNull();
        }

        [Fact]
        public void ForEvidence_WithEmptySubmissionId_ShouldThrow()
        {
            Action act = () => ScoreEntry.ForEvidence(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                ScoreValue.Award(100), Guid.Empty, DateTime.UtcNow);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ForPenalty_ShouldStoreReasonAndNegativeValue()
        {
            var reason = new PenaltyReason("Retraso");

            var entry = ScoreEntry.ForPenalty(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                ScoreValue.Penalty(20), reason, DateTime.UtcNow);

            entry.Origin.Should().Be(ScoreOrigin.Penalty);
            entry.Value.Points.Should().Be(-20);
            entry.Reason!.Value.Should().Be("Retraso");
        }
    }

    public class SessionEventTests
    {
        [Fact]
        public void Record_WithValidData_ShouldCreate()
        {
            var sessionId = Guid.NewGuid();

            var evt = SessionEvent.Record(Guid.NewGuid(), sessionId, "PenaltyApplied", "desc", DateTime.UtcNow, "operador");

            evt.SessionId.Should().Be(sessionId);
            evt.Type.Should().Be("PenaltyApplied");
            evt.Actor.Should().Be("operador");
        }

        [Fact]
        public void Record_WithEmptyType_ShouldThrow()
        {
            Action act = () => SessionEvent.Record(Guid.NewGuid(), Guid.NewGuid(), "", "desc", DateTime.UtcNow);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Record_WithEmptySession_ShouldThrow()
        {
            Action act = () => SessionEvent.Record(Guid.NewGuid(), Guid.Empty, "Type", "desc", DateTime.UtcNow);
            act.Should().Throw<ArgumentException>();
        }
    }
}
