using System;
using FluentAssertions;
using ScoringService.Domain.Events;
using Xunit;

namespace ScoringService.Tests.Domain
{
    public class PenaltyAppliedDomainEventTests
    {
        [Fact]
        public void Constructor_WithValidArguments_SetsAllProperties()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            const int points = 10;
            const string reason = "Pista usada sin permiso";
            var occurredAt = DateTime.UtcNow;

            // Act
            var @event = new PenaltyAppliedDomainEvent(sessionId, teamId, points, reason, occurredAt);

            // Assert
            @event.SessionId.Should().Be(sessionId);
            @event.TeamId.Should().Be(teamId);
            @event.Points.Should().Be(points);
            @event.Reason.Should().Be(reason);
            @event.OccurredAt.Should().Be(occurredAt);
        }

        [Fact]
        public void TwoEvents_WithSameValues_AreEqual()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            var occurredAt = DateTime.UtcNow;

            var event1 = new PenaltyAppliedDomainEvent(sessionId, teamId, 5, "Razón A", occurredAt);
            var event2 = new PenaltyAppliedDomainEvent(sessionId, teamId, 5, "Razón A", occurredAt);

            // Assert (record structural equality)
            event1.Should().Be(event2);
        }

        [Fact]
        public void TwoEvents_WithDifferentPoints_AreNotEqual()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            var occurredAt = DateTime.UtcNow;

            var event1 = new PenaltyAppliedDomainEvent(sessionId, teamId, 5, "Razón A", occurredAt);
            var event2 = new PenaltyAppliedDomainEvent(sessionId, teamId, 10, "Razón A", occurredAt);

            // Assert
            event1.Should().NotBe(event2);
        }

        [Fact]
        public void Deconstruct_ReturnsExpectedValues()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            var occurredAt = DateTime.UtcNow;
            var @event = new PenaltyAppliedDomainEvent(sessionId, teamId, 15, "Trampa detectada", occurredAt);

            // Act
            var (sid, tid, pts, rsn, occ) = @event;

            // Assert
            sid.Should().Be(sessionId);
            tid.Should().Be(teamId);
            pts.Should().Be(15);
            rsn.Should().Be("Trampa detectada");
            occ.Should().Be(occurredAt);
        }

        [Fact]
        public void ToString_ContainsMeaningfulInfo()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var @event = new PenaltyAppliedDomainEvent(sessionId, Guid.NewGuid(), 20, "Foul", DateTime.UtcNow);

            // Act
            var str = @event.ToString();

            // Assert
            str.Should().Contain(sessionId.ToString());
        }
    }
}
