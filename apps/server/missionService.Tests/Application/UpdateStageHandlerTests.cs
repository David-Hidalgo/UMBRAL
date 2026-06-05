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
using MissionService.Domain.RepositoryInterfaces;
using Xunit;

namespace MissionService.Tests.Application
{
    public class UpdateStageHandlerTests
    {
        private readonly Mock<IMissionRepository> _repositoryMock;
        private readonly UpdateStageHandler _handler;

        public UpdateStageHandlerTests()
        {
            _repositoryMock = new Mock<IMissionRepository>(MockBehavior.Strict);
            _handler = new UpdateStageHandler(_repositoryMock.Object);
        }

        [Fact]
        public async Task Handle_Should_Update_Stage_Successfully()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var stageId = Guid.NewGuid();
            var stage = Stage.Create(stageId, "Old Name", "Old Desc", 1, new List<SubStage>());
            var mission = Mission.Create(missionId, "Mission", "Desc", DifficultyLevel.Beginner, 60, new List<Stage> { stage });

            var command = new UpdateStageCommand(
                missionId,
                stageId,
                "New Name",
                "New Desc",
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
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            stage.Name.Should().Be("New Name");
            stage.Description.Should().Be("New Desc");
            stage.SubStages.Should().HaveCount(1);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Throw_MissionNotFoundException_When_Mission_DoesNotExist()
        {
            // Arrange
            var command = new UpdateStageCommand(Guid.NewGuid(), Guid.NewGuid(), "Name", "Desc", new List<CreateSubStageCommand>());

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
            var command = new UpdateStageCommand(missionId, Guid.NewGuid(), "Name", "Desc", new List<CreateSubStageCommand>());

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
            var stageId = Guid.NewGuid();
            var stage = Stage.Create(stageId, "Name", "Desc", 1, new List<SubStage>());
            stage.IncrementSessionCount(); // Lock it
            var mission = Mission.Create(missionId, "Mission", "Desc", DifficultyLevel.Beginner, 60, new List<Stage> { stage });
            var command = new UpdateStageCommand(missionId, stageId, "New Name", "New Desc", new List<CreateSubStageCommand>());

            _repositoryMock.Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mission);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<StageActiveException>();
        }

        [Fact]
        public async Task Handle_Should_Throw_ArgumentException_When_TriviaSubStage_Has_No_Question()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var stageId = Guid.NewGuid();
            var stage = Stage.Create(stageId, "Name", "Desc", 1, new List<SubStage>());
            var mission = Mission.Create(missionId, "Mission", "Desc", DifficultyLevel.Beginner, 60, new List<Stage> { stage });

            var command = new UpdateStageCommand(
                missionId,
                stageId,
                "New Name",
                "New Desc",
                new List<CreateSubStageCommand>
                {
                    new CreateSubStageCommand("Trivia", 0, "Trivia", 10, null, null, null, null)
                }
            );

            _repositoryMock.Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mission);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*must have a question*");
        }

        [Fact]
        public async Task Handle_Should_Update_Stage_With_TreasureHunt_SubStage_When_Type_Is_TreasureHunt()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var stageId = Guid.NewGuid();
            var stage = Stage.Create(stageId, "Old Name", "Old Desc", 1, new List<SubStage>());
            var mission = Mission.Create(missionId, "Mission", "Desc", DifficultyLevel.Beginner, 60, new List<Stage> { stage });

            var command = new UpdateStageCommand(
                missionId,
                stageId,
                "New Name",
                "New Desc",
                new List<CreateSubStageCommand>
                {
                    new CreateSubStageCommand("TH SubStage", 0, "TreasureHunt", 20, null, "Find the hidden object", "SECRETCODE123", null)
                }
            );

            _repositoryMock.Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mission);
            _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            stage.SubStages.Should().HaveCount(1);
            stage.SubStages.First().Should().BeOfType<TreasureHuntSubStage>();
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Update_Stage_With_Clues_When_SubStage_Has_Non_Empty_Clues()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var stageId = Guid.NewGuid();
            var stage = Stage.Create(stageId, "Old Name", "Old Desc", 1, new List<SubStage>());
            var mission = Mission.Create(missionId, "Mission", "Desc", DifficultyLevel.Beginner, 60, new List<Stage> { stage });

            var command = new UpdateStageCommand(
                missionId,
                stageId,
                "New Name",
                "New Desc",
                new List<CreateSubStageCommand>
                {
                    new CreateSubStageCommand(
                        "Trivia SubStage",
                        0,
                        "Trivia",
                        10,
                        new CreateQuestionCommand("What is 2+2?", new List<CreateAnswerCommand> { new CreateAnswerCommand("4", true) }),
                        null,
                        null,
                        new List<CreateClueCommand> { new CreateClueCommand("Think about basic math", 1) }
                    )
                }
            );

            _repositoryMock.Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mission);
            _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            stage.SubStages.Should().HaveCount(1);
            stage.SubStages.First().Clues.Should().HaveCount(1);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
