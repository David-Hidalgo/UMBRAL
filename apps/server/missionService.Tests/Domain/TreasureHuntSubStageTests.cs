using System;
using System.Collections.Generic;
using FluentAssertions;
using MissionService.Domain.Entities;
using Xunit;

namespace MissionService.Tests.Domain
{
    public class TreasureHuntSubStageTests
    {
        [Fact]
        public void Clue_Should_Be_Created_Successfully_Without_PenaltyPoints()
        {
            // Arrange & Act
            var clue = Clue.Create(Guid.NewGuid(), "Look under the red chair", 1);

            // Assert
            clue.Should().NotBeNull();
            clue.Text.Should().Be("Look under the red chair");
            clue.Order.Should().Be(1);
        }

        [Fact]
        public void TreasureHuntSubStage_Should_Be_Created_Successfully_With_SecretCode()
        {
            // Arrange
            var clues = new List<Clue>
            {
                Clue.Create(Guid.NewGuid(), "Under the table", 0)
            };

            // Act
            var substage = TreasureHuntSubStage.Create(
                Guid.NewGuid(),
                "Find the Secret Key",
                0,
                100,
                "Find the key to escape",
                "secret-key-123",
                clues
            );

            // Assert
            substage.Should().NotBeNull();
            substage.Title.Should().Be("Find the Secret Key");
            substage.Points.Should().Be(100);
            substage.Objective.Should().Be("Find the key to escape");
            substage.SecretCode.Should().Be("SECRET-KEY-123"); // Upper-case normalization
            substage.Clues.Should().HaveCount(1);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void TreasureHuntSubStage_Should_Throw_ArgumentException_When_SecretCode_Is_Empty(string? invalidCode)
        {
            // Arrange
            var clues = new List<Clue>
            {
                Clue.Create(Guid.NewGuid(), "Under the table", 0)
            };

            // Act
            Action act = () => TreasureHuntSubStage.Create(
                Guid.NewGuid(),
                "Find the Secret Key",
                0,
                100,
                "Find the key to escape",
                invalidCode!,
                clues
            );

            // Assert
            act.Should().Throw<ArgumentException>().WithMessage("*Secret code cannot be empty*");
        }

        [Theory]
        [InlineData("secret key")] // has space
        [InlineData("secret@123")] // special character
        [InlineData("secret_key")] // underscore is not allowed in alphanumeric/hyphen match
        public void TreasureHuntSubStage_Should_Throw_ArgumentException_When_SecretCode_Is_Invalid_Format(string invalidCode)
        {
            // Arrange
            var clues = new List<Clue>
            {
                Clue.Create(Guid.NewGuid(), "Under the table", 0)
            };

            // Act
            Action act = () => TreasureHuntSubStage.Create(
                Guid.NewGuid(),
                "Find the Secret Key",
                0,
                100,
                "Find the key to escape",
                invalidCode,
                clues
            );

            // Assert
            act.Should().Throw<ArgumentException>().WithMessage("*must be alphanumeric and can contain hyphens*");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void TreasureHuntSubStage_Should_Throw_ArgumentException_When_Title_Is_Invalid(string? invalidTitle)
        {
            // Arrange
            var clues = new List<Clue> { Clue.Create(Guid.NewGuid(), "Under the table", 0) };

            // Act
            Action act = () => TreasureHuntSubStage.Create(
                Guid.NewGuid(), invalidTitle!, 0, 100, "Find the key to escape", "SECRET-123", clues);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void TreasureHuntSubStage_Should_Throw_ArgumentException_When_Points_Are_Negative()
        {
            // Arrange
            var clues = new List<Clue> { Clue.Create(Guid.NewGuid(), "Under the table", 0) };

            // Act
            Action act = () => TreasureHuntSubStage.Create(
                Guid.NewGuid(), "Find the Secret Key", 0, -1, "Find the key to escape", "SECRET-123", clues);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void TreasureHuntSubStage_Should_Throw_ArgumentException_When_Objective_Is_Invalid(string? invalidObjective)
        {
            // Arrange
            var clues = new List<Clue> { Clue.Create(Guid.NewGuid(), "Under the table", 0) };

            // Act
            Action act = () => TreasureHuntSubStage.Create(
                Guid.NewGuid(), "Find the Secret Key", 0, 100, invalidObjective!, "SECRET-123", clues);

            // Assert
            act.Should().Throw<ArgumentException>();
        }
    }
}
