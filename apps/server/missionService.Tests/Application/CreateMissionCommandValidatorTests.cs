using System;
using System.Collections.Generic;
using FluentAssertions;
using MissionService.Application.Commands;
using MissionService.Application.Validators;
using Xunit;

namespace MissionService.Tests.Application
{
    public class CreateMissionCommandValidatorTests
    {
        private readonly CreateMissionCommandValidator _validator = new();

        [Fact]
        public void Should_Be_Valid_When_All_Fields_Are_Correct()
        {
            // Arrange
            var command = new CreateMissionCommand(
                "Mission 1",
                "Mission description",
                new List<CreateStageCommand>
                {
                    new CreateStageCommand(
                        "Stage 1",
                        "Stage description",
                        0,
                        new List<CreateSubStageCommand>
                        {
                            new CreateSubStageCommand(
                                "Trivia SubStage",
                                0,
                                "Trivia",
                                10,
                                new CreateQuestionCommand(
                                    "Question 1",
                                    new List<CreateAnswerCommand>
                                    {
                                        new CreateAnswerCommand("Answer 1", true),
                                        new CreateAnswerCommand("Answer 2", false)
                                    }
                                ),
                                null,
                                null
                            )
                        }
                    )
                }
            );

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Should_Be_Invalid_When_Name_Is_Empty()
        {
            // Arrange
            var command = new CreateMissionCommand(
                "",
                "Description",
                new List<CreateStageCommand>()
            );

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Name");
        }

        [Fact]
        public void Should_Be_Invalid_When_Trivia_Question_Has_No_Correct_Answers()
        {
            // Arrange
            var command = new CreateMissionCommand(
                "Mission 1",
                "Description",
                new List<CreateStageCommand>
                {
                    new CreateStageCommand(
                        "Stage 1",
                        "Stage description",
                        0,
                        new List<CreateSubStageCommand>
                        {
                            new CreateSubStageCommand(
                                "Trivia SubStage",
                                0,
                                "Trivia",
                                10,
                                new CreateQuestionCommand(
                                    "Question 1",
                                    new List<CreateAnswerCommand>
                                    {
                                        new CreateAnswerCommand("Answer 1", false),
                                        new CreateAnswerCommand("Answer 2", false)
                                    }
                                ),
                                null,
                                null
                            )
                        }
                    )
                }
            );

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains("at least one correct answer"));
        }

        [Fact]
        public void Should_Be_Invalid_When_SubStage_Points_Are_Negative()
        {
            // Arrange
            var command = new CreateMissionCommand(
                "Mission 1",
                "Description",
                new List<CreateStageCommand>
                {
                    new CreateStageCommand(
                        "Stage 1",
                        "Stage description",
                        0,
                        new List<CreateSubStageCommand>
                        {
                            new CreateSubStageCommand(
                                "Trivia SubStage",
                                0,
                                "Trivia",
                                -5,
                                new CreateQuestionCommand(
                                    "Question 1",
                                    new List<CreateAnswerCommand>
                                    {
                                        new CreateAnswerCommand("Answer 1", true)
                                    }
                                ),
                                null,
                                null
                            )
                        }
                    )
                }
            );

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName.Contains("Points"));
        }

        [Fact]
        public void Should_Be_Invalid_When_Trivia_Has_No_Question()
        {
            // Arrange
            var command = new CreateMissionCommand(
                "Mission 1",
                "Description",
                new List<CreateStageCommand>
                {
                    new CreateStageCommand(
                        "Stage 1",
                        "Stage description",
                        0,
                        new List<CreateSubStageCommand>
                        {
                            new CreateSubStageCommand(
                                "Trivia SubStage",
                                0,
                                "Trivia",
                                10,
                                null,
                                null,
                                null
                            )
                        }
                    )
                }
            );

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName.Contains("Question"));
        }
    }
}
