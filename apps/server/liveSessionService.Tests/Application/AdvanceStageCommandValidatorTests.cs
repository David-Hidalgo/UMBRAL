using System;
using FluentAssertions;
using Xunit;
using LiveSessionService.Application.Commands;
using LiveSessionService.Application.Validators;

namespace LiveSessionService.Tests.Application
{
    public class AdvanceStageCommandValidatorTests
    {
        private readonly AdvanceStageCommandValidator _validator = new();

        [Fact]
        public void Should_Be_Valid_When_All_Fields_Are_Correct()
        {
            // Arrange
            var command = new AdvanceStageCommand(Guid.NewGuid(), 1, "jwt-token");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Should_Be_Invalid_When_SessionId_Is_Empty()
        {
            // Arrange
            var command = new AdvanceStageCommand(Guid.Empty, 1, "jwt-token");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(AdvanceStageCommand.SessionId));
        }

        [Fact]
        public void Should_Be_Invalid_When_NextStageIndex_Is_Negative()
        {
            // Arrange
            var command = new AdvanceStageCommand(Guid.NewGuid(), -1, "jwt-token");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(AdvanceStageCommand.NextStageIndex));
        }

        [Fact]
        public void Should_Be_Invalid_When_JwtToken_Is_Empty()
        {
            // Arrange
            var command = new AdvanceStageCommand(Guid.NewGuid(), 0, "");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(AdvanceStageCommand.JwtToken));
        }
    }
}
