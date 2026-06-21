using System;
using FluentAssertions;
using Xunit;
using LiveSessionService.Application.Commands;
using LiveSessionService.Application.Validators;

namespace LiveSessionService.Tests.Application
{
    public class JoinTeamCommandValidatorTests
    {
        private readonly JoinTeamCommandValidator _validator = new();

        [Fact]
        public void Should_Be_Valid_When_All_Fields_Are_Correct()
        {
            // Arrange
            var command = new JoinTeamCommand(Guid.NewGuid(), Guid.NewGuid(), "user-1", "Juan");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Should_Be_Invalid_When_SessionId_Is_Empty()
        {
            // Arrange
            var command = new JoinTeamCommand(Guid.Empty, Guid.NewGuid(), "user-1", "Juan");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(JoinTeamCommand.SessionId));
        }

        [Fact]
        public void Should_Be_Invalid_When_TeamId_Is_Empty()
        {
            // Arrange
            var command = new JoinTeamCommand(Guid.NewGuid(), Guid.Empty, "user-1", "Juan");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(JoinTeamCommand.TeamId));
        }

        [Fact]
        public void Should_Be_Invalid_When_KeycloakUserId_Is_Empty()
        {
            // Arrange
            var command = new JoinTeamCommand(Guid.NewGuid(), Guid.NewGuid(), "", "Juan");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(JoinTeamCommand.KeycloakUserId));
        }

        [Fact]
        public void Should_Be_Invalid_When_DisplayName_Is_Empty()
        {
            // Arrange
            var command = new JoinTeamCommand(Guid.NewGuid(), Guid.NewGuid(), "user-1", "");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(JoinTeamCommand.DisplayName));
        }

        [Fact]
        public void Should_Be_Invalid_When_DisplayName_Exceeds_MaxLength()
        {
            // Arrange
            var command = new JoinTeamCommand(Guid.NewGuid(), Guid.NewGuid(), "user-1", new string('A', 101));

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(JoinTeamCommand.DisplayName));
        }
    }
}
