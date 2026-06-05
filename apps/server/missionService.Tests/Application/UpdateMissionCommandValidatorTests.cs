using System;
using FluentAssertions;
using MissionService.Application.Commands;
using MissionService.Application.Validators;
using Xunit;

namespace MissionService.Tests.Application
{
    public class UpdateMissionCommandValidatorTests
    {
        private readonly UpdateMissionCommandValidator _validator = new();

        [Fact]
        public void Should_Be_Valid_When_All_Optional_Fields_Are_Provided()
        {
            // Arrange
            var command = new UpdateMissionCommand(Guid.NewGuid(), "New Name", "New Description", "Active", "Beginner", 90);

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Should_Be_Valid_When_Only_Name_Is_Provided()
        {
            // Arrange
            var command = new UpdateMissionCommand(Guid.NewGuid(), "New Name", null, null, null, null);

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Should_Be_Invalid_When_Id_Is_Empty()
        {
            // Arrange
            var command = new UpdateMissionCommand(Guid.Empty, "New Name", null, null, null, null);

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Id");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Should_Be_Invalid_When_Name_Is_Present_But_Empty(string invalidName)
        {
            // Arrange
            var command = new UpdateMissionCommand(Guid.NewGuid(), invalidName, null, null, null, null);

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Name");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Should_Be_Invalid_When_Description_Is_Present_But_Empty(string invalidDescription)
        {
            // Arrange
            var command = new UpdateMissionCommand(Guid.NewGuid(), null, invalidDescription, null, null, null);

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Description");
        }

        [Theory]
        [InlineData("Published")]
        [InlineData("Pending")]
        [InlineData("")]
        public void Should_Be_Invalid_When_State_Is_Invalid(string invalidState)
        {
            // Arrange
            var command = new UpdateMissionCommand(Guid.NewGuid(), null, null, invalidState, null, null);

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "State");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Should_Be_Invalid_When_MaxTimeMinutes_Is_Not_Positive(int invalidTime)
        {
            // Arrange
            var command = new UpdateMissionCommand(Guid.NewGuid(), null, null, null, null, invalidTime);

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "MaxTimeMinutes");
        }

        [Fact]
        public void Should_Be_Invalid_When_No_Field_Is_Provided()
        {
            // Arrange
            var command = new UpdateMissionCommand(Guid.NewGuid(), null, null, null, null, null);

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
        }
    }
}
