using System;
using System.Collections.Generic;
using FluentAssertions;
using MissionService.Domain.Entities;
using Xunit;

namespace MissionService.Tests.Domain
{
    public class StageTests
    {
        [Fact]
        public void Create_Should_Succeed_With_Valid_Parameters()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var stage = Stage.Create(id, "Stage 1", "First room", 0, new List<SubStage>());

            // Assert
            stage.Should().NotBeNull();
            stage.Id.Should().Be(id);
            stage.Name.Should().Be("Stage 1");
            stage.Description.Should().Be("First room");
            stage.Order.Should().Be(0);
            stage.SubStages.Should().BeEmpty();
        }

        [Fact]
        public void Create_Should_Throw_ArgumentException_When_Id_Is_Empty()
        {
            // Act
            Action act = () => Stage.Create(Guid.Empty, "Stage 1", "First room", 0, new List<SubStage>());

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Create_Should_Throw_ArgumentException_When_Name_Is_Invalid(string? invalidName)
        {
            // Act
            Action act = () => Stage.Create(Guid.NewGuid(), invalidName!, "First room", 0, new List<SubStage>());

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Create_Should_Throw_ArgumentException_When_Description_Is_Invalid(string? invalidDescription)
        {
            // Act
            Action act = () => Stage.Create(Guid.NewGuid(), "Stage 1", invalidDescription!, 0, new List<SubStage>());

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_Should_Throw_ArgumentException_When_Order_Is_Negative()
        {
            // Act
            Action act = () => Stage.Create(Guid.NewGuid(), "Stage 1", "First room", -1, new List<SubStage>());

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Update_Should_Replace_Name_Description_And_SubStages()
        {
            // Arrange
            var stage = Stage.Create(Guid.NewGuid(), "Stage 1", "First room", 0, new List<SubStage> { ValidSubStage() });
            var replacement = ValidSubStage();

            // Act
            stage.Update("Stage 2", "Second room", new List<SubStage> { replacement });

            // Assert
            stage.Name.Should().Be("Stage 2");
            stage.Description.Should().Be("Second room");
            stage.SubStages.Should().ContainSingle().Which.Should().Be(replacement);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Update_With_SubStages_Should_Throw_ArgumentException_When_Name_Is_Invalid(string? invalidName)
        {
            // Arrange
            var stage = ValidStage();

            // Act
            Action act = () => stage.Update(invalidName!, "Second room", new List<SubStage>());

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Update_With_SubStages_Should_Throw_ArgumentException_When_Description_Is_Invalid(string? invalidDescription)
        {
            // Arrange
            var stage = ValidStage();

            // Act
            Action act = () => stage.Update("Stage 2", invalidDescription!, new List<SubStage>());

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Update_Should_Modify_Name_And_Description()
        {
            // Arrange
            var stage = ValidStage();

            // Act
            stage.Update("Stage 2", "Second room");

            // Assert
            stage.Name.Should().Be("Stage 2");
            stage.Description.Should().Be("Second room");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Update_Should_Throw_ArgumentException_When_Name_Is_Invalid(string? invalidName)
        {
            // Arrange
            var stage = ValidStage();

            // Act
            Action act = () => stage.Update(invalidName!, "Second room");

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Update_Should_Throw_ArgumentException_When_Description_Is_Invalid(string? invalidDescription)
        {
            // Arrange
            var stage = ValidStage();

            // Act
            Action act = () => stage.Update("Stage 2", invalidDescription!);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void SetOrder_Should_Throw_ArgumentException_When_Order_Is_Negative()
        {
            // Arrange
            var stage = ValidStage();

            // Act
            Action act = () => stage.SetOrder(-1);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void AddSubStage_Should_Throw_ArgumentNullException_When_SubStage_Is_Null()
        {
            // Arrange
            var stage = ValidStage();

            // Act
            Action act = () => stage.AddSubStage(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void RemoveSubStage_Should_Remove_The_Matching_SubStage()
        {
            // Arrange
            var subStage = ValidSubStage();
            var stage = Stage.Create(Guid.NewGuid(), "Stage 1", "First room", 0, new List<SubStage> { subStage });

            // Act
            stage.RemoveSubStage(subStage.Id);

            // Assert
            stage.SubStages.Should().BeEmpty();
        }

        [Fact]
        public void RemoveSubStage_Should_Do_Nothing_When_SubStage_Is_Not_Found()
        {
            // Arrange
            var stage = Stage.Create(Guid.NewGuid(), "Stage 1", "First room", 0, new List<SubStage> { ValidSubStage() });

            // Act
            stage.RemoveSubStage(Guid.NewGuid());

            // Assert
            stage.SubStages.Should().HaveCount(1);
        }

        private static Stage ValidStage() =>
            Stage.Create(Guid.NewGuid(), "Stage 1", "First room", 0, new List<SubStage>());

        private static TriviaSubStage ValidSubStage() =>
            TriviaSubStage.Create(
                Guid.NewGuid(),
                "Trivia Round 1",
                0,
                10,
                Question.Create(
                    Guid.NewGuid(),
                    "What is the capital of France?",
                    new List<Answer> { Answer.Create(Guid.NewGuid(), "Paris", true) }
                )
            );
    }
}
