using System;
using System.Linq;
using FluentAssertions;
using ScoringService.Domain.Entities;
using ScoringService.Domain.Enums;
using ScoringService.Domain.Events;
using ScoringService.Domain.Exceptions;
using Xunit;

namespace ScoringService.Tests.Domain
{
    public class EvidenceSubmissionTests
    {
        private static EvidenceSubmission CreatePending()
            => EvidenceSubmission.Create(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                SubmissionType.Trivia, "respuesta", DateTime.UtcNow);

        [Fact]
        public void Create_WithValidData_ShouldStartPending()
        {
            var submission = CreatePending();

            submission.Status.Should().Be(SubmissionStatus.Pending);
            submission.AwardedPoints.Should().BeNull();
            submission.ValidatedAt.Should().BeNull();
        }

        [Fact]
        public void Create_ShouldTrimPayload()
        {
            var submission = EvidenceSubmission.Create(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                SubmissionType.TreasureHunt, "  codigo  ", DateTime.UtcNow);

            submission.Payload.Should().Be("codigo");
        }

        [Theory]
        [InlineData(true, false, false, "x")]   // sessionId vacío
        [InlineData(false, true, false, "x")]   // teamId vacío
        [InlineData(false, false, true, "x")]   // subStageId vacío
        [InlineData(false, false, false, "")]   // payload vacío
        public void Create_WithInvalidData_ShouldThrow(bool emptySession, bool emptyTeam, bool emptySub, string payload)
        {
            Action act = () => EvidenceSubmission.Create(
                Guid.NewGuid(),
                emptySession ? Guid.Empty : Guid.NewGuid(),
                emptyTeam ? Guid.Empty : Guid.NewGuid(),
                emptySub ? Guid.Empty : Guid.NewGuid(),
                SubmissionType.Trivia, payload, DateTime.UtcNow);

            act.Should().Throw<InvalidSubmissionException>();
        }

        [Fact]
        public void MarkCorrect_ShouldAwardPointsAndRegisterDomainEvent()
        {
            var submission = CreatePending();
            var at = DateTime.UtcNow;

            submission.MarkCorrect(150, at);

            submission.Status.Should().Be(SubmissionStatus.Correct);
            submission.AwardedPoints.Should().Be(150);
            submission.ValidatedAt.Should().Be(at);
            submission.DomainEvents.Should().ContainSingle(e => e is EvidenceValidatedDomainEvent);
            submission.DomainEvents.OfType<EvidenceValidatedDomainEvent>().Single().IsCorrect.Should().BeTrue();
        }

        [Fact]
        public void MarkIncorrect_ShouldSetZeroPointsAndRegisterDomainEvent()
        {
            var submission = CreatePending();

            submission.MarkIncorrect(DateTime.UtcNow);

            submission.Status.Should().Be(SubmissionStatus.Incorrect);
            submission.AwardedPoints.Should().Be(0);
            submission.DomainEvents.OfType<EvidenceValidatedDomainEvent>().Single().IsCorrect.Should().BeFalse();
        }

        [Fact]
        public void MarkCorrect_WhenAlreadyValidated_ShouldThrow()
        {
            var submission = CreatePending();
            submission.MarkCorrect(10, DateTime.UtcNow);

            Action act = () => submission.MarkCorrect(20, DateTime.UtcNow);

            act.Should().Throw<SubmissionAlreadyValidatedException>();
        }

        [Fact]
        public void MarkCorrect_WithNegativePoints_ShouldThrow()
        {
            var submission = CreatePending();

            Action act = () => submission.MarkCorrect(-1, DateTime.UtcNow);

            act.Should().Throw<InvalidSubmissionException>();
        }

        [Fact]
        public void ClearDomainEvents_ShouldEmptyTheList()
        {
            var submission = CreatePending();
            submission.MarkCorrect(10, DateTime.UtcNow);

            submission.ClearDomainEvents();

            submission.DomainEvents.Should().BeEmpty();
        }
    }
}
