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
    }
}
