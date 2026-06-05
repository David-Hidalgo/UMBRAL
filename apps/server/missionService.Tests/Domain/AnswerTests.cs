using System;
using FluentAssertions;
using MissionService.Domain.Entities;
using Xunit;

namespace MissionService.Tests.Domain
{
    public class AnswerTests
    {
        [Fact]
        public void Create_Should_Succeed_When_IsCorrect_Is_True()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var answer = Answer.Create(id, "Paris", true);

            // Assert
            answer.Should().NotBeNull();
            answer.Id.Should().Be(id);
            answer.Text.Should().Be("Paris");
            answer.IsCorrect.Should().BeTrue();
        }

        [Fact]
        public void Create_Should_Succeed_When_IsCorrect_Is_False()
        {
            // Act
            var answer = Answer.Create(Guid.NewGuid(), "London", false);

            // Assert
            answer.IsCorrect.Should().BeFalse();
        }

        [Fact]
        public void Create_Should_Throw_ArgumentException_When_Id_Is_Empty()
        {
            // Act
            Action act = () => Answer.Create(Guid.Empty, "Paris", true);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Create_Should_Throw_ArgumentException_When_Text_Is_Invalid(string? invalidText)
        {
            // Act
            Action act = () => Answer.Create(Guid.NewGuid(), invalidText!, true);

            // Assert
            act.Should().Throw<ArgumentException>();
        }
    }
}
