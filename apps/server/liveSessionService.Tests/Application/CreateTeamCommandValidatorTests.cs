using System;
using FluentAssertions;
using Xunit;
using LiveSessionService.Application.Commands;
using LiveSessionService.Application.Validators;

namespace LiveSessionService.Tests.Application
{
    public class CreateTeamCommandValidatorTests
    {
        private readonly CreateTeamCommandValidator _validator = new();

        [Fact]
        public void Should_Be_Valid_When_All_Fields_Are_Correct()
        {
            // Arrange
            var command = new CreateTeamCommand(Guid.NewGuid(), "Equipo Alfa");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Should_Be_Invalid_When_SessionId_Is_Empty()
        {
            // Arrange
            var command = new CreateTeamCommand(Guid.Empty, "Equipo Alfa");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTeamCommand.SessionId));
        }

        [Fact]
        public void Should_Be_Invalid_When_TeamName_Is_Empty()
        {
            // Arrange
            var command = new CreateTeamCommand(Guid.NewGuid(), "");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTeamCommand.TeamName));
        }

        [Fact]
        public void Should_Be_Invalid_When_TeamName_Exceeds_MaxLength()
        {
            // Arrange
            var command = new CreateTeamCommand(Guid.NewGuid(), new string('A', 51));

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTeamCommand.TeamName));
        }
    }
}
