using System;
using FluentAssertions;
using ScoringService.Application.Commands;
using ScoringService.Application.Validators;
using ScoringService.Domain.Enums;
using Xunit;

namespace ScoringService.Tests.Application
{
    public class ValidatorTests
    {
        private readonly SubmitEvidenceCommandValidator _submitValidator = new();
        private readonly ApplyPenaltyCommandValidator _penaltyValidator = new();
        private readonly ValidateEvidenceCommandValidator _validateValidator = new();

        [Fact]
        public void SubmitEvidence_WithValidCommand_ShouldPass()
        {
            var command = new SubmitEvidenceCommand(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), SubmissionType.Trivia, "respuesta", "token");

            _submitValidator.Validate(command).IsValid.Should().BeTrue();
        }

        [Fact]
        public void SubmitEvidence_WithEmptyPayload_ShouldFail()
        {
            var command = new SubmitEvidenceCommand(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), SubmissionType.Trivia, "", "token");

            _submitValidator.Validate(command).IsValid.Should().BeFalse();
        }

        [Fact]
        public void SubmitEvidence_WithEmptyIds_ShouldFail()
        {
            var command = new SubmitEvidenceCommand(
                Guid.Empty, Guid.Empty, Guid.Empty, SubmissionType.Trivia, "x", "token");

            _submitValidator.Validate(command).IsValid.Should().BeFalse();
        }

        [Fact]
        public void ApplyPenalty_WithValidCommand_ShouldPass()
        {
            var command = new ApplyPenaltyCommand(Guid.NewGuid(), Guid.NewGuid(), 10, "Motivo", "op");

            _penaltyValidator.Validate(command).IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(0, "Motivo")]      // puntos no positivos
        [InlineData(10, "")]           // motivo vacío (RB-06)
        public void ApplyPenalty_WithInvalidData_ShouldFail(int points, string reason)
        {
            var command = new ApplyPenaltyCommand(Guid.NewGuid(), Guid.NewGuid(), points, reason, "op");

            _penaltyValidator.Validate(command).IsValid.Should().BeFalse();
        }

        [Fact]
        public void ValidateEvidence_WithEmptyIds_ShouldFail()
        {
            var command = new ValidateEvidenceCommand(Guid.Empty, Guid.Empty, true, "op", "token");

            _validateValidator.Validate(command).IsValid.Should().BeFalse();
        }
    }
}
