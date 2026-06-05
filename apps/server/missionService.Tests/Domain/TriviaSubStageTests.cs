using System;
using System.Collections.Generic;
using FluentAssertions;
using MissionService.Domain.Entities;
using Xunit;

namespace MissionService.Tests.Domain
{
    public class TriviaSubStageTests
    {
        private static Question ValidQuestion() =>
            Question.Create(
                Guid.NewGuid(),
                "What is the capital of France?",
                new List<Answer> { Answer.Create(Guid.NewGuid(), "Paris", true) }
            );

        [Fact]
        public void Create_Should_Succeed_With_Valid_Parameters()
        {
            // Arrange
            var id = Guid.NewGuid();
            var question = ValidQuestion();

            // Act
            var subStage = TriviaSubStage.Create(id, "Trivia Round 1", 0, 10, question);

            // Assert
            subStage.Should().NotBeNull();
            subStage.Id.Should().Be(id);
            subStage.Title.Should().Be("Trivia Round 1");
            subStage.Order.Should().Be(0);
            subStage.Points.Should().Be(10);
            subStage.Question.Should().Be(question);
        }

        [Fact]
        public void Create_Should_Throw_ArgumentException_When_Id_Is_Empty()
        {
            // Act
            Action act = () => TriviaSubStage.Create(Guid.Empty, "Trivia Round 1", 0, 10, ValidQuestion());

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Create_Should_Throw_ArgumentException_When_Title_Is_Invalid(string? invalidTitle)
        {
            // Act
            Action act = () => TriviaSubStage.Create(Guid.NewGuid(), invalidTitle!, 0, 10, ValidQuestion());

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_Should_Throw_ArgumentException_When_Points_Are_Negative()
        {
            // Act
            Action act = () => TriviaSubStage.Create(Guid.NewGuid(), "Trivia Round 1", 0, -1, ValidQuestion());

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_Should_Throw_ArgumentNullException_When_Question_Is_Null()
        {
            // Act
            Action act = () => TriviaSubStage.Create(Guid.NewGuid(), "Trivia Round 1", 0, 10, null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Create_Should_Include_Clues_When_Provided()
        {
            // Arrange
            var clues = new List<Clue>
            {
                Clue.Create(Guid.NewGuid(), "Look under the table", 0)
            };

            // Act
            var subStage = TriviaSubStage.Create(Guid.NewGuid(), "Trivia Round 1", 0, 10, ValidQuestion(), clues);

            // Assert
            subStage.Clues.Should().HaveCount(1);
        }
    }
}
