using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using MissionService.Application.Commands;
using MissionService.Application.Handlers;
using MissionService.Domain.Entities;
using MissionService.Domain.Enums;
using MissionService.Domain.Exceptions;
using MissionService.Domain.RepositoryInterfaces;
using Xunit;

namespace MissionService.Tests.Application
{
    public class DeleteStageHandlerTests
    {
        private readonly Mock<IMissionRepository> _repositoryMock;
        private readonly DeleteStageHandler _handler;

        public DeleteStageHandlerTests()
        {
            _repositoryMock = new Mock<IMissionRepository>(MockBehavior.Strict);
            _handler = new DeleteStageHandler(_repositoryMock.Object);
        }

        [Fact]
        public async Task Handle_Should_Delete_Stage_Successfully()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var stageId1 = Guid.NewGuid();
            var stageId2 = Guid.NewGuid();
            var stage1 = Stage.Create(stageId1, "Stage 1", "Desc", 1, new List<SubStage>());
            var stage2 = Stage.Create(stageId2, "Stage 2", "Desc", 2, new List<SubStage>());
            var mission = Mission.Create(missionId, "Mission", "Desc", DifficultyLevel.Beginner, 60, new List<Stage> { stage1, stage2 });
            var command = new DeleteStageCommand(missionId, stageId1);

            _repositoryMock.Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mission);

            _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            mission.Stages.Should().HaveCount(1);
            mission.Stages.Should().NotContain(stage1);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Throw_MissionNotFoundException_When_Mission_DoesNotExist()
        {
            // Arrange
            var command = new DeleteStageCommand(Guid.NewGuid(), Guid.NewGuid());

            _repositoryMock.Setup(r => r.GetByIdAsync(command.MissionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Mission?)null);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<MissionNotFoundException>();
        }

        [Fact]
        public async Task Handle_Should_Throw_StageNotFoundException_When_Stage_DoesNotExist()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var mission = Mission.Create(missionId, "Mission", "Desc", DifficultyLevel.Beginner, 60, new List<Stage>());
            var command = new DeleteStageCommand(missionId, Guid.NewGuid());

            _repositoryMock.Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mission);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<StageNotFoundException>();
        }

        [Fact]
        public async Task Handle_Should_Throw_StageActiveException_When_Stage_Is_Active()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var stageId1 = Guid.NewGuid();
            var stageId2 = Guid.NewGuid();
            var subStage = TreasureHuntSubStage.Create(Guid.NewGuid(), "SubStage", 1, 10, "Objective", "CODE-1", null);
            var stage1 = Stage.Create(stageId1, "Stage 1", "Desc", 1, new List<SubStage> { subStage });
            var stage2 = Stage.Create(stageId2, "Stage 2", "Desc", 2, new List<SubStage>());
            var mission = Mission.Create(missionId, "Mission", "Desc", DifficultyLevel.Beginner, 60, new List<Stage> { stage1, stage2 });
            subStage.IncrementSessionCount(); // Lock it
            var command = new DeleteStageCommand(missionId, stageId1);

            _repositoryMock.Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mission);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<StageActiveException>();
        }

        [Fact]
        public async Task Handle_Should_Throw_CannotRemoveLastStageException_When_Trying_To_Remove_Last_Stage()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var stageId = Guid.NewGuid();
            var stage = Stage.Create(stageId, "Stage 1", "Desc", 1, new List<SubStage>());
            var mission = Mission.Create(missionId, "Mission", "Desc", DifficultyLevel.Beginner, 60, new List<Stage> { stage });
            var command = new DeleteStageCommand(missionId, stageId);

            _repositoryMock.Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mission);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<CannotRemoveLastStageException>();
        }
    }
}
