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
    }
}
