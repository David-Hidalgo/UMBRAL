using System;
using FluentAssertions;
using Xunit;
using LiveSessionService.Application.Commands;
using LiveSessionService.Application.Validators;

namespace LiveSessionService.Tests.Application
{
    public class CreateLiveSessionCommandValidatorTests
    {
        private readonly CreateLiveSessionCommandValidator _validator = new();

        [Fact]
        public void Should_Be_Valid_When_All_Fields_Are_Correct()
        {
            // Arrange
            var command = new CreateLiveSessionCommand(Guid.NewGuid(), "Partida 1", null, "jwt-token");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Should_Be_Invalid_When_MissionId_Is_Empty()
        {
            // Arrange
            var command = new CreateLiveSessionCommand(Guid.Empty, "Partida 1", null, "jwt-token");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLiveSessionCommand.MissionId));
        }

        [Fact]
        public void Should_Be_Invalid_When_Name_Is_Empty()
        {
            // Arrange
            var command = new CreateLiveSessionCommand(Guid.NewGuid(), "", null, "jwt-token");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLiveSessionCommand.Name));
        }

        [Fact]
        public void Should_Be_Invalid_When_Name_Exceeds_MaxLength()
        {
            // Arrange
            var command = new CreateLiveSessionCommand(Guid.NewGuid(), new string('A', 101), null, "jwt-token");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLiveSessionCommand.Name));
        }

        [Fact]
        public void Should_Be_Invalid_When_JwtToken_Is_Empty()
        {
            // Arrange
            var command = new CreateLiveSessionCommand(Guid.NewGuid(), "Partida 1", null, "");

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLiveSessionCommand.JwtToken));
        }
    }
}
