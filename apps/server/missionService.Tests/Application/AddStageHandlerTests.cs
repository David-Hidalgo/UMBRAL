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
    public class AddStageHandlerTests
    {
        private readonly Mock<IMissionRepository> _repositoryMock;
        private readonly AddStageHandler _handler;

        public AddStageHandlerTests()
        {
            _repositoryMock = new Mock<IMissionRepository>(MockBehavior.Strict);
            _handler = new AddStageHandler(_repositoryMock.Object);
        }

        [Fact]
        public async Task Handle_Should_Add_Stage_Successfully()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var stage = Stage.Create(Guid.NewGuid(), "Stage 1", "Desc", 1, new List<SubStage>());
            var mission = Mission.Create(missionId, "Mission", "Desc", DifficultyLevel.Beginner, 60, new List<Stage> { stage });

            var command = new AddStageCommand(
                missionId,
                "Stage 2",
                "Stage 2 Desc",
                2,
                new List<CreateSubStageCommand>
                {
                    new CreateSubStageCommand(
                        "Trivia SubStage",
                        0,
                        "Trivia",
                        10,
                        new CreateQuestionCommand(
                            "What is 2+2?",
                            new List<CreateAnswerCommand> { new CreateAnswerCommand("4", true), new CreateAnswerCommand("5", false) }
                        ),
                        null,
                        null,
                        null
                    )
                }
            );

            _repositoryMock.Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mission);

            _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var stageId = await _handler.Handle(command, CancellationToken.None);

            // Assert
            stageId.Should().NotBeEmpty();
            mission.Stages.Should().HaveCount(2);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Throw_MissionNotFoundException_When_Mission_DoesNotExist()
        {
            // Arrange
            var command = new AddStageCommand(Guid.NewGuid(), "Stage", "Desc", 1, new List<CreateSubStageCommand>());

            _repositoryMock.Setup(r => r.GetByIdAsync(command.MissionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Mission?)null);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<MissionNotFoundException>();
        }

        [Fact]
        public async Task Handle_Should_Throw_MissionActiveException_When_Mission_Is_Active()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var mission = Mission.Create(missionId, "Mission", "Desc", DifficultyLevel.Beginner, 60, new List<Stage>());
            mission.IncrementSessionCount(); // Lock it
            var command = new AddStageCommand(missionId, "Stage", "Desc", 1, new List<CreateSubStageCommand>());

            _repositoryMock.Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mission);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<MissionActiveException>();
        }

        [Fact]
        public async Task Handle_Should_Throw_ArgumentException_When_Stage_Order_Is_Duplicate()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var stage = Stage.Create(Guid.NewGuid(), "Stage 1", "Desc", 1, new List<SubStage>());
            var mission = Mission.Create(missionId, "Mission", "Desc", DifficultyLevel.Beginner, 60, new List<Stage> { stage });
            var command = new AddStageCommand(missionId, "Stage 2", "Desc", 1, new List<CreateSubStageCommand>());

            _repositoryMock.Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mission);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*order '1' already exists*");
        }

        [Fact]
        public async Task Handle_Should_Throw_ArgumentException_When_Stage_Name_Is_Duplicate()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var stage = Stage.Create(Guid.NewGuid(), "Stage 1", "Desc", 1, new List<SubStage>());
            var mission = Mission.Create(missionId, "Mission", "Desc", DifficultyLevel.Beginner, 60, new List<Stage> { stage });
            var command = new AddStageCommand(missionId, "Stage 1", "Desc", 2, new List<CreateSubStageCommand>());

            _repositoryMock.Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mission);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*name 'Stage 1' already exists*");
        }
    }
}
