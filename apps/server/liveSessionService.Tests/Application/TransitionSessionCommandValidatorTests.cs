using System;
using FluentAssertions;
using Xunit;
using LiveSessionService.Application.Commands;
using LiveSessionService.Application.Validators;
using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Tests.Application
{
    public class TransitionSessionCommandValidatorTests
    {
        private readonly TransitionSessionCommandValidator _validator = new();

        [Fact]
        public void Should_Be_Valid_When_All_Fields_Are_Correct()
        {
            // Arrange
            var command = new TransitionSessionCommand(Guid.NewGuid(), LiveSessionStatus.Activa, "jwt-token");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Should_Be_Invalid_When_SessionId_Is_Empty()
        {
            // Arrange
            var command = new TransitionSessionCommand(Guid.Empty, LiveSessionStatus.Activa, "jwt-token");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(TransitionSessionCommand.SessionId));
        }

        [Fact]
        public void Should_Be_Invalid_When_TargetStatus_Is_Not_In_Enum()
        {
            // Arrange
            var command = new TransitionSessionCommand(Guid.NewGuid(), (LiveSessionStatus)999, "jwt-token");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(TransitionSessionCommand.TargetStatus));
        }

        [Fact]
        public void Should_Be_Invalid_When_JwtToken_Is_Empty()
        {
            // Arrange
            var command = new TransitionSessionCommand(Guid.NewGuid(), LiveSessionStatus.Activa, "");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(TransitionSessionCommand.JwtToken));
        }
    }
}
