using System;
using FluentAssertions;
using MissionService.Domain.Entities;
using Xunit;

namespace MissionService.Tests.Domain
{
    public class ClueTests
    {
        [Fact]
        public void Create_Should_Succeed_With_Valid_Parameters()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var clue = Clue.Create(id, "Look under the red chair", 1);

            // Assert
            clue.Should().NotBeNull();
            clue.Id.Should().Be(id);
            clue.Text.Should().Be("Look under the red chair");
            clue.Order.Should().Be(1);
        }

        [Fact]
        public void Create_Should_Throw_ArgumentException_When_Id_Is_Empty()
        {
            // Act
            Action act = () => Clue.Create(Guid.Empty, "Look under the red chair", 0);

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
            Action act = () => Clue.Create(Guid.NewGuid(), invalidText!, 0);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_Should_Throw_ArgumentException_When_Order_Is_Negative()
        {
            // Act
            Action act = () => Clue.Create(Guid.NewGuid(), "Look under the red chair", -1);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Update_Should_Modify_Text_And_Order()
        {
            // Arrange
            var clue = Clue.Create(Guid.NewGuid(), "Look under the red chair", 1);

            // Act
            clue.Update("Look behind the blue door", 3);

            // Assert
            clue.Text.Should().Be("Look behind the blue door");
            clue.Order.Should().Be(3);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Update_Should_Throw_ArgumentException_When_Text_Is_Invalid(string? invalidText)
        {
            // Arrange
            var clue = Clue.Create(Guid.NewGuid(), "Look under the red chair", 1);

            // Act
            Action act = () => clue.Update(invalidText!, 1);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Update_Should_Throw_ArgumentException_When_Order_Is_Negative()
        {
            // Arrange
            var clue = Clue.Create(Guid.NewGuid(), "Look under the red chair", 1);

            // Act
            Action act = () => clue.Update("Look behind the blue door", -1);

            // Assert
            act.Should().Throw<ArgumentException>();
        }
    }
}
