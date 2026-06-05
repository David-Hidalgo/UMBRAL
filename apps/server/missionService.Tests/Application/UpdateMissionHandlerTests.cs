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
    public class UpdateMissionHandlerTests
    {
        private readonly Mock<IMissionRepository> _repositoryMock;
        private readonly UpdateMissionHandler _handler;

        public UpdateMissionHandlerTests()
        {
            _repositoryMock = new Mock<IMissionRepository>(MockBehavior.Strict);
            _handler = new UpdateMissionHandler(_repositoryMock.Object);
        }

        [Fact]
        public async Task Handle_Should_Update_Mission_Info_Successfully()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var mission = Mission.Create(missionId, "Old Name", "Old Desc", DifficultyLevel.Beginner, 30, new List<Stage>());
            var command = new UpdateMissionCommand(missionId, "New Name", "New Desc", null, "Intermediate", 45);

            _repositoryMock.Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mission);

            _repositoryMock.Setup(r => r.ExistsByNameExcludingIdAsync("New Name", missionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            mission.Name.Should().Be("New Name");
            mission.Description.Should().Be("New Desc");
            mission.DifficultyLevel.Should().Be(DifficultyLevel.Intermediate);
            mission.MaxTimeMinutes.Should().Be(45);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Throw_MissionNotFoundException_When_Mission_DoesNotExist()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var command = new UpdateMissionCommand(missionId, "Name", "Desc", null, "Beginner", 60);

            _repositoryMock.Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Mission?)null);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<MissionNotFoundException>();
        }

        [Fact]
        public async Task Handle_Should_Throw_DuplicateMissionNameException_When_Name_Exists()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var mission = Mission.Create(missionId, "Old Name", "Old Desc", DifficultyLevel.Beginner, 30, new List<Stage>());
            var command = new UpdateMissionCommand(missionId, "Existing Name", "Desc", null, "Beginner", 60);

            _repositoryMock.Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mission);

            _repositoryMock.Setup(r => r.ExistsByNameExcludingIdAsync("Existing Name", missionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DuplicateMissionNameException>();
        }

        [Fact]
        public async Task Handle_Should_Activate_Mission_Successfully()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var mission = Mission.Create(missionId, "Name", "Desc", DifficultyLevel.Beginner, 30, new List<Stage>());
            var command = new UpdateMissionCommand(missionId, null, null, "Active", null, null);

            _repositoryMock.Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mission);

            _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            mission.State.Should().Be(MissionState.Active);
        }

        [Fact]
        public async Task Handle_Should_Deactivate_Mission_Successfully()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var mission = Mission.Create(missionId, "Name", "Desc", DifficultyLevel.Beginner, 30, new List<Stage>());
            mission.Activate();
            var command = new UpdateMissionCommand(missionId, null, null, "Inactive", null, null);

            _repositoryMock.Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mission);

            _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            mission.State.Should().Be(MissionState.Inactive);
        }
    }
}
