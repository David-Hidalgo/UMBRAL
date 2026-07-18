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

        [Fact]
        public void Create_Should_Throw_ArgumentNullException_When_Clues_Contains_Null()
        {
            // Arrange
            var clues = new List<Clue> { null! };

            // Act
            Action act = () => TriviaSubStage.Create(Guid.NewGuid(), "Trivia Round 1", 0, 10, ValidQuestion(), clues);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void SessionCount_Operations_Should_Modify_Status_Correctly()
        {
            // Arrange
            var subStage = TriviaSubStage.Create(Guid.NewGuid(), "Trivia Round 1", 0, 10, ValidQuestion());

            // Act & Assert
            subStage.Status.ActiveSessionCount.Should().Be(0);

            subStage.IncrementSessionCount();
            subStage.Status.ActiveSessionCount.Should().Be(1);

            subStage.DecrementSessionCount();
            subStage.Status.ActiveSessionCount.Should().Be(0);
        }

        [Fact]
        public void Update_Should_Modify_Title_Order_And_Points()
        {
            // Arrange
            var subStage = TriviaSubStage.Create(Guid.NewGuid(), "Trivia Round 1", 0, 10, ValidQuestion());

            // Act
            subStage.Update("Trivia Round 2", 5, 25);

            // Assert
            subStage.Title.Should().Be("Trivia Round 2");
            subStage.Order.Should().Be(5);
            subStage.Points.Should().Be(25);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Update_Should_Throw_ArgumentException_When_Title_Is_Invalid(string? invalidTitle)
        {
            // Arrange
            var subStage = TriviaSubStage.Create(Guid.NewGuid(), "Trivia Round 1", 0, 10, ValidQuestion());

            // Act
            Action act = () => subStage.Update(invalidTitle!, 0, 10);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Update_Should_Throw_ArgumentException_When_Points_Are_Negative()
        {
            // Arrange
            var subStage = TriviaSubStage.Create(Guid.NewGuid(), "Trivia Round 1", 0, 10, ValidQuestion());

            // Act
            Action act = () => subStage.Update("Trivia Round 2", 0, -1);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void UpdateClues_Should_Remove_Clues_Missing_From_The_New_List()
        {
            // Arrange
            var first = Clue.Create(Guid.NewGuid(), "Look under the table", 0);
            var second = Clue.Create(Guid.NewGuid(), "Check the bookshelf", 1);
            var subStage = TriviaSubStage.Create(
                Guid.NewGuid(), "Trivia Round 1", 0, 10, ValidQuestion(), new List<Clue> { first, second });

            // Act
            subStage.UpdateClues(new List<Clue> { first });

            // Assert
            subStage.Clues.Should().ContainSingle().Which.Should().Be(first);
        }

        [Fact]
        public void UpdateClues_Should_Add_Clues_Not_Previously_Present()
        {
            // Arrange
            var first = Clue.Create(Guid.NewGuid(), "Look under the table", 0);
            var subStage = TriviaSubStage.Create(
                Guid.NewGuid(), "Trivia Round 1", 0, 10, ValidQuestion(), new List<Clue> { first });
            var second = Clue.Create(Guid.NewGuid(), "Check the bookshelf", 1);

            // Act
            subStage.UpdateClues(new List<Clue> { first, second });

            // Assert
            subStage.Clues.Should().HaveCount(2).And.Contain(second);
        }
    }
}

