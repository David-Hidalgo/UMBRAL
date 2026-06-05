using System;
using System.Collections.Generic;
using FluentAssertions;
using MissionService.Application.Commands;
using MissionService.Application.Validators;
using Xunit;

namespace MissionService.Tests.Application
{
    public class AddStageCommandValidatorTests
    {
        private readonly AddStageCommandValidator _validator = new();

        private static List<CreateSubStageCommand> ValidSubStages() =>
            new()
            {
                new CreateSubStageCommand(
                    "Trivia SubStage", 0, "Trivia", 10,
                    new CreateQuestionCommand("Question?", new List<CreateAnswerCommand>
                    {
                        new CreateAnswerCommand("Answer 1", true),
                        new CreateAnswerCommand("Answer 2", false)
                    }),
                    null, null, null)
            };

        [Fact]
        public void Should_Be_Valid_When_All_Fields_Are_Correct()
        {
            // Arrange
            var command = new AddStageCommand(Guid.NewGuid(), "Stage 1", "Stage description", 0, ValidSubStages());

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Should_Be_Invalid_When_MissionId_Is_Empty()
        {
            // Arrange
            var command = new AddStageCommand(Guid.Empty, "Stage 1", "Stage description", 0, ValidSubStages());

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "MissionId");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Should_Be_Invalid_When_Name_Is_Empty(string invalidName)
        {
            // Arrange
            var command = new AddStageCommand(Guid.NewGuid(), invalidName, "Stage description", 0, ValidSubStages());

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Name");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Should_Be_Invalid_When_Description_Is_Empty(string invalidDescription)
        {
            // Arrange
            var command = new AddStageCommand(Guid.NewGuid(), "Stage 1", invalidDescription, 0, ValidSubStages());

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Description");
        }

        [Fact]
        public void Should_Be_Invalid_When_Order_Is_Negative()
        {
            // Arrange
            var command = new AddStageCommand(Guid.NewGuid(), "Stage 1", "Stage description", -1, ValidSubStages());

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Order");
        }

        [Fact]
        public void Should_Be_Invalid_When_SubStages_Is_Empty()
        {
            // Arrange
            var command = new AddStageCommand(Guid.NewGuid(), "Stage 1", "Stage description", 0, new List<CreateSubStageCommand>());

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "SubStages");
        }
    }
}
