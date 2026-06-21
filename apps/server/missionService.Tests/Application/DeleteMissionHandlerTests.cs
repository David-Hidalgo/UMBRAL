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
using MissionService.Domain.Repositories;
using Xunit;

namespace MissionService.Tests.Application
{
    public class DeleteMissionHandlerTests
    {
        private readonly Mock<IMissionRepository> _repositoryMock;
        private readonly DeleteMissionHandler _handler;

        public DeleteMissionHandlerTests()
        {
            _repositoryMock = new Mock<IMissionRepository>(MockBehavior.Strict);
            _handler = new DeleteMissionHandler(_repositoryMock.Object);
        }

        [Fact]
        public async Task Handle_Should_Delete_Mission_Successfully()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var mission = Mission.Create(missionId, "Mission", "Desc", DifficultyLevel.Beginner, 60, new List<Stage>());
            var command = new DeleteMissionCommand(missionId);

            _repositoryMock.Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mission);

            _repositoryMock.Setup(r => r.Remove(mission));
            _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _repositoryMock.Verify(r => r.Remove(mission), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Throw_MissionNotFoundException_When_Mission_DoesNotExist()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var command = new DeleteMissionCommand(missionId);

            _repositoryMock.Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Mission?)null);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<MissionNotFoundException>();
        }

        [Fact]
        public async Task Handle_Should_Throw_MissionHasActiveSessionsException_When_Mission_Has_Active_Sessions()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var subStage = TreasureHuntSubStage.Create(Guid.NewGuid(), "SubStage", 1, 10, "Objective", "CODE-1", null);
            var stage = Stage.Create(Guid.NewGuid(), "Stage 1", "Description", 0, new List<SubStage> { subStage });
            var mission = Mission.Create(missionId, "Mission", "Desc", DifficultyLevel.Beginner, 60, new List<Stage> { stage });
            subStage.IncrementSessionCount();
            var command = new DeleteMissionCommand(missionId);

            _repositoryMock.Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mission);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<MissionHasActiveSessionsException>();
        }

        [Fact]
        public async Task Handle_Should_Throw_MissionHasActiveSessionsException_When_A_Stage_Has_Active_Sessions()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var subStage = TreasureHuntSubStage.Create(Guid.NewGuid(), "SubStage", 1, 10, "Objective", "CODE-1", null);
            var stage = Stage.Create(Guid.NewGuid(), "Stage 1", "Description", 0, new List<SubStage> { subStage });
            var mission = Mission.Create(missionId, "Mission", "Desc", DifficultyLevel.Beginner, 60, new List<Stage> { stage });
            subStage.IncrementSessionCount();
            var command = new DeleteMissionCommand(missionId);

            _repositoryMock.Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mission);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<MissionHasActiveSessionsException>();
        }
    }
}
