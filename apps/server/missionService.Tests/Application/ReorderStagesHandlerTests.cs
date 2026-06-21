using System;
using System.Collections.Generic;
using System.Linq;
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
    public class ReorderStagesHandlerTests
    {
        private readonly Mock<IMissionRepository> _repositoryMock;
        private readonly ReorderStagesHandler _handler;

        public ReorderStagesHandlerTests()
        {
            _repositoryMock = new Mock<IMissionRepository>(MockBehavior.Strict);
            _handler = new ReorderStagesHandler(_repositoryMock.Object);
        }

        [Fact]
        public async Task Handle_Should_Reorder_Stages_Successfully_And_Set_1Based_Order()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var stageId1 = Guid.NewGuid();
            var stageId2 = Guid.NewGuid();
            var stage1 = Stage.Create(stageId1, "Stage 1", "Desc", 1, new List<SubStage>());
            var stage2 = Stage.Create(stageId2, "Stage 2", "Desc", 2, new List<SubStage>());
            var mission = Mission.Create(missionId, "Mission", "Desc", DifficultyLevel.Beginner, 60, new List<Stage> { stage1, stage2 });
            
            // Reorder so stage 2 comes first
            var command = new ReorderStagesCommand(missionId, new List<Guid> { stageId2, stageId1 });

            _repositoryMock.Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mission);

            _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            stage2.Order.Should().Be(1);
            stage1.Order.Should().Be(2);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Throw_MissionNotFoundException_When_Mission_DoesNotExist()
        {
            // Arrange
            var command = new ReorderStagesCommand(Guid.NewGuid(), new List<Guid>());

            _repositoryMock.Setup(r => r.GetByIdAsync(command.MissionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Mission?)null);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<MissionNotFoundException>();
        }

        [Fact]
        public async Task Handle_Should_Throw_ArgumentException_When_StageIds_DoNotMatch()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var stageId1 = Guid.NewGuid();
            var stage1 = Stage.Create(stageId1, "Stage 1", "Desc", 1, new List<SubStage>());
            var mission = Mission.Create(missionId, "Mission", "Desc", DifficultyLevel.Beginner, 60, new List<Stage> { stage1 });
            
            // Reorder command with invalid non-matching ID
            var command = new ReorderStagesCommand(missionId, new List<Guid> { Guid.NewGuid() });

            _repositoryMock.Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mission);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }
    }
}
