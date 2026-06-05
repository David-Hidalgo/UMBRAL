using System;
using System.Collections.Generic;
using FluentAssertions;
using MissionService.Domain.Entities;
using MissionService.Domain.Enums;
using MissionService.Domain.Exceptions;
using MissionService.Domain.ValueObjects;
using Xunit;

namespace MissionService.Tests.Domain
{
    public class MissionTests
    {
        [Fact]
        public void Mission_Should_Be_Created_With_Inactive_State()
        {
            // Arrange & Act
            var mission = Mission.Create(
                Guid.NewGuid(), 
                "Escape Room", 
                "Solve mysteries to escape.", 
                DifficultyLevel.Beginner, 
                60, 
                new List<Stage>());

            // Assert
            mission.Should().NotBeNull();
            mission.State.Should().Be(MissionState.Inactive);
            mission.Status.ActiveSessionCount.Should().Be(0);
            mission.DifficultyLevel.Should().Be(DifficultyLevel.Beginner);
            mission.MaxTimeMinutes.Should().Be(60);
        }

        [Fact]
        public void AddStage_Should_Add_Stage_Successfully_When_Mission_Has_No_Active_Sessions()
        {
            // Arrange
            var mission = Mission.Create(
                Guid.NewGuid(), 
                "Escape Room", 
                "Solve mysteries to escape.", 
                DifficultyLevel.Beginner, 
                60, 
                new List<Stage>());
            var stage = Stage.Create(Guid.NewGuid(), "Stage 1", "First room", 0, new List<SubStage>());

            // Act
            mission.AddStage(stage);

            // Assert
            mission.Stages.Should().Contain(stage);
        }

        [Fact]
        public void AddStage_Should_Throw_MissionActiveException_When_Mission_Has_Active_Sessions()
        {
            // Arrange
            var mission = Mission.Create(
                Guid.NewGuid(), 
                "Escape Room", 
                "Solve mysteries to escape.", 
                DifficultyLevel.Beginner, 
                60, 
                new List<Stage>());
            mission.IncrementSessionCount();
            var stage = Stage.Create(Guid.NewGuid(), "Stage 1", "First room", 0, new List<SubStage>());

            // Act
            Action act = () => mission.AddStage(stage);

            // Assert
            act.Should().Throw<MissionActiveException>();
        }

        [Fact]
        public void AddStage_Should_Throw_ArgumentException_For_Duplicate_Stage_Names()
        {
            // Arrange
            var stage1 = Stage.Create(Guid.NewGuid(), "Stage 1", "First room", 0, new List<SubStage>());
            var stage2 = Stage.Create(Guid.NewGuid(), "Stage 1", "Second room", 1, new List<SubStage>()); // Duplicate Name

            var mission = Mission.Create(
                Guid.NewGuid(), 
                "Escape Room", 
                "Solve mysteries.", 
                DifficultyLevel.Beginner, 
                60, 
                new List<Stage> { stage1 });

            // Act
            Action act = () => mission.AddStage(stage2);

            // Assert
            act.Should().Throw<ArgumentException>().WithMessage("*already exists in this mission*");
        }

        [Fact]
        public void AddStage_Should_Throw_ArgumentException_For_Duplicate_Stage_Orders()
        {
            // Arrange
            var stage1 = Stage.Create(Guid.NewGuid(), "Stage 1", "First room", 0, new List<SubStage>());
            var stage2 = Stage.Create(Guid.NewGuid(), "Stage 2", "Second room", 0, new List<SubStage>()); // Duplicate Order

            var mission = Mission.Create(
                Guid.NewGuid(), 
                "Escape Room", 
                "Solve mysteries.", 
                DifficultyLevel.Beginner, 
                60, 
                new List<Stage> { stage1 });

            // Act
            Action act = () => mission.AddStage(stage2);

            // Assert
            act.Should().Throw<ArgumentException>().WithMessage("*already exists in this mission*");
        }
    }
}
