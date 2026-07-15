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
                "Beginner",
                60,
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
                "Beginner",
                60,
                new List<CreateStageCommand>()
            );

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Name");
        }

        [Fact]
        public void Should_Be_Invalid_When_DifficultyLevel_Is_Invalid()
        {
            // Arrange
            var command = new CreateMissionCommand(
                "Mission 1",
                "Description",
                "InvalidLevel",
                60,
                new List<CreateStageCommand>()
            );

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "DifficultyLevel");
        }

        [Fact]
        public void Should_Be_Invalid_When_MaxTimeMinutes_Is_Zero_Or_Negative()
        {
            // Arrange
            var command = new CreateMissionCommand(
                "Mission 1",
                "Description",
                "Beginner",
                0,
                new List<CreateStageCommand>()
            );

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "MaxTimeMinutes");
        }

        [Fact]
        public void Should_Be_Invalid_When_Trivia_Question_Has_No_Correct_Answers()
        {
            // Arrange
            var command = new CreateMissionCommand(
                "Mission 1",
                "Description",
                "Beginner",
                60,
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
                "Beginner",
                60,
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

        private static CreateMissionCommand BuildTreasureHuntCommand(
            double? latitude, double? longitude, int? searchRadiusMeters)
        {
            return new CreateMissionCommand(
                "Mission 1",
                "Description",
                "Beginner",
                60,
                new List<CreateStageCommand>
                {
                    new CreateStageCommand(
                        "Stage 1",
                        "Stage description",
                        0,
                        new List<CreateSubStageCommand>
                        {
                            new CreateSubStageCommand(
                                "Treasure SubStage",
                                0,
                                "TreasureHunt",
                                10,
                                Question: null,
                                Objective: "Encuentra el cofre",
                                SecretCode: "SECRET-123",
                                Latitude: latitude,
                                Longitude: longitude,
                                SearchRadiusMeters: searchRadiusMeters)
                        }
                    )
                }
            );
        }

        [Fact]
        public void Should_Be_Valid_When_TreasureHunt_Has_Consistent_Location()
        {
            var command = BuildTreasureHuntCommand(10.488158, -66.846534, 120);

            var result = _validator.Validate(command);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Should_Be_Valid_When_TreasureHunt_Has_No_Location()
        {
            var command = BuildTreasureHuntCommand(null, null, null);

            var result = _validator.Validate(command);

            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(91.0, 0.0)]    // latitud fuera de rango
        [InlineData(-91.0, 0.0)]
        [InlineData(0.0, 181.0)]   // longitud fuera de rango
        [InlineData(0.0, -181.0)]
        public void Should_Be_Invalid_When_Coordinates_Out_Of_Range(double lat, double lon)
        {
            var command = BuildTreasureHuntCommand(lat, lon, null);

            var result = _validator.Validate(command);

            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public void Should_Be_Invalid_When_Only_One_Coordinate_Provided()
        {
            var command = BuildTreasureHuntCommand(10.5, null, null);

            var result = _validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains("together"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void Should_Be_Invalid_When_SearchRadius_Not_Positive(int radius)
        {
            var command = BuildTreasureHuntCommand(10.5, -66.8, radius);

            var result = _validator.Validate(command);

            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public void Should_Be_Invalid_When_SearchRadius_Provided_Without_Location()
        {
            var command = BuildTreasureHuntCommand(null, null, 120);

            var result = _validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains("requires a treasure location"));
        }

        [Fact]
        public void Should_Be_Invalid_When_Trivia_Has_No_Question()
        {
            // Arrange
            var command = new CreateMissionCommand(
                "Mission 1",
                "Description",
                "Beginner",
                60,
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
