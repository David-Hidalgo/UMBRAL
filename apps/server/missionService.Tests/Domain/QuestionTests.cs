using System;
using System.Collections.Generic;
using FluentAssertions;
using MissionService.Domain.Entities;
using MissionService.Domain.Exceptions;
using Xunit;

namespace MissionService.Tests.Domain
{
    public class QuestionTests
    {
        [Fact]
        public void Question_Should_Be_Created_With_Valid_Parameters()
        {
            // Arrange
            var id = Guid.NewGuid();
            var text = "What is the capital of France?";
            var answers = new List<Answer>
            {
                Answer.Create(Guid.NewGuid(), "Paris", true),
                Answer.Create(Guid.NewGuid(), "London", false)
            };

            // Act
            var question = Question.Create(id, text, answers);

            // Assert
            question.Should().NotBeNull();
            question.Id.Should().Be(id);
            question.Text.Should().Be(text);
            question.Answers.Should().HaveCount(2);
        }

        [Fact]
        public void Creation_Should_Throw_TriviaRequiresCorrectAnswerException_When_No_Answers_Are_Correct()
        {
            // Arrange
            var id = Guid.NewGuid();
            var text = "What is the capital of France?";
            var answers = new List<Answer>
            {
                Answer.Create(Guid.NewGuid(), "Paris", false),
                Answer.Create(Guid.NewGuid(), "London", false)
            };

            // Act
            Action act = () => Question.Create(id, text, answers);

            // Assert
            act.Should().Throw<TriviaRequiresCorrectAnswerException>();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Creation_Should_Throw_ArgumentException_When_Text_Is_Invalid(string? invalidText)
        {
            // Arrange
            var answers = new List<Answer> { Answer.Create(Guid.NewGuid(), "Paris", true) };

            // Act
            Action act = () => Question.Create(Guid.NewGuid(), invalidText!, answers);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Creation_Should_Throw_ArgumentException_When_Answers_Are_Empty()
        {
            // Act
            Action act = () => Question.Create(Guid.NewGuid(), "Question text", new List<Answer>());

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Creation_Should_Throw_ArgumentException_When_Id_Is_Empty()
        {
            // Arrange
            var answers = new List<Answer> { Answer.Create(Guid.NewGuid(), "Paris", true) };

            // Act
            Action act = () => Question.Create(Guid.Empty, "Question text", answers);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Creation_Should_Throw_ArgumentNullException_When_Answers_Contain_Null()
        {
            // Arrange: la respuesta correcta va primero para que la validacion de
            // "al menos una correcta" pase y el null llegue hasta AddAnswer.
            var answers = new List<Answer> { Answer.Create(Guid.NewGuid(), "Paris", true), null! };

            // Act
            Action act = () => Question.Create(Guid.NewGuid(), "Question text", answers);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Update_Should_Throw_ArgumentException_When_Text_Is_Invalid(string? invalidText)
        {
            // Arrange
            var question = ValidQuestion();

            // Act
            Action act = () => question.Update(invalidText!, new List<Answer> { Answer.Create(Guid.NewGuid(), "Paris", true) });

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Update_Should_Throw_ArgumentException_When_Answers_Are_Empty()
        {
            // Arrange
            var question = ValidQuestion();

            // Act
            Action act = () => question.Update("New text", new List<Answer>());

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Update_Should_Throw_TriviaRequiresCorrectAnswerException_When_No_Answer_Is_Correct()
        {
            // Arrange
            var question = ValidQuestion();
            var answers = new List<Answer>
            {
                Answer.Create(Guid.NewGuid(), "London", false),
                Answer.Create(Guid.NewGuid(), "Madrid", false)
            };

            // Act
            Action act = () => question.Update("New text", answers);

            // Assert
            act.Should().Throw<TriviaRequiresCorrectAnswerException>();
        }

        [Fact]
        public void Update_Should_Remove_Answers_Missing_From_The_New_List()
        {
            // Arrange
            var paris = Answer.Create(Guid.NewGuid(), "Paris", true);
            var london = Answer.Create(Guid.NewGuid(), "London", false);
            var question = Question.Create(Guid.NewGuid(), "Capital of France?", new List<Answer> { paris, london });

            // Act
            question.Update("Capital of France?", new List<Answer> { paris });

            // Assert
            question.Answers.Should().ContainSingle().Which.Should().Be(paris);
        }

        [Fact]
        public void Update_Should_Add_Answers_Not_Previously_Present()
        {
            // Arrange
            var paris = Answer.Create(Guid.NewGuid(), "Paris", true);
            var question = Question.Create(Guid.NewGuid(), "Capital of France?", new List<Answer> { paris });
            var madrid = Answer.Create(Guid.NewGuid(), "Madrid", false);

            // Act
            question.Update("Capital of France?", new List<Answer> { paris, madrid });

            // Assert
            question.Answers.Should().HaveCount(2).And.Contain(madrid);
        }

        private static Question ValidQuestion() =>
            Question.Create(
                Guid.NewGuid(),
                "What is the capital of France?",
                new List<Answer> { Answer.Create(Guid.NewGuid(), "Paris", true) }
            );
    }
}
