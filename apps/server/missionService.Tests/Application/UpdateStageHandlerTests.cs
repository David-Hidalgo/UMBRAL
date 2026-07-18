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
        public async Task Handle_Should_Update_Stage_With_TreasureHunt_And_Clues_Successfully()
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
                        "Treasure SubStage",
                        0,
                        "TreasureHunt",
                        20,
                        null,
                        "Find secret code",
                        "SECRET-123",
                        new List<CreateClueCommand>
                        {
                            new CreateClueCommand("Clue text", 1)
                        }
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
            var subStage = stage.SubStages.Single() as TreasureHuntSubStage;
            subStage.Should().NotBeNull();
            subStage!.Title.Should().Be("Treasure SubStage");
            subStage.SecretCode.Should().Be("SECRET-123");
            subStage.Clues.Should().HaveCount(1);
            subStage.Clues.Single().Text.Should().Be("Clue text");
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
            var subStage = TreasureHuntSubStage.Create(Guid.NewGuid(), "SubStage", 1, 10, "Objective", "CODE-1", null);
            var stage = Stage.Create(stageId, "Name", "Desc", 1, new List<SubStage> { subStage });
            var mission = Mission.Create(missionId, "Mission", "Desc", DifficultyLevel.Beginner, 60, new List<Stage> { stage });
            subStage.IncrementSessionCount(); // Lock it
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
        public async Task Handle_Should_Update_SubStage_InPlace_Successfully()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var stageId = Guid.NewGuid();
            var subStageId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var answerId = Guid.NewGuid();
            var clueId = Guid.NewGuid();

            var answer = Answer.Create(answerId, "Old Answer", true);
            var question = Question.Create(questionId, "Old Question?", new List<Answer> { answer });
            var clue = Clue.Create(clueId, "Old Clue", 1);
            var subStage = TriviaSubStage.Create(subStageId, "Old Title", 1, 10, question, new List<Clue> { clue });

            var stage = Stage.Create(stageId, "Old Stage Name", "Old Stage Desc", 1, new List<SubStage> { subStage });
            var mission = Mission.Create(missionId, "Mission", "Desc", DifficultyLevel.Beginner, 60, new List<Stage> { stage });

            var command = new UpdateStageCommand(
                missionId,
                stageId,
                "New Stage Name",
                "New Stage Desc",
                new List<CreateSubStageCommand>
                {
                    new CreateSubStageCommand(
                        "New Title",
                        1,
                        "Trivia",
                        20,
                        new CreateQuestionCommand(
                            "New Question?",
                            new List<CreateAnswerCommand> { new CreateAnswerCommand("New Answer", true, answerId) },
                            questionId
                        ),
                        null,
                        null,
                        new List<CreateClueCommand>
                        {
                            new CreateClueCommand("New Clue", 1, clueId)
                        },
                        subStageId
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
            stage.Name.Should().Be("New Stage Name");
            stage.Description.Should().Be("New Stage Desc");
            stage.SubStages.Should().HaveCount(1);

            var updatedSubStage = stage.SubStages.Single() as TriviaSubStage;
            updatedSubStage.Should().NotBeNull();
            updatedSubStage!.Id.Should().Be(subStageId); // ID preserved
            updatedSubStage.Title.Should().Be("New Title");
            updatedSubStage.Points.Should().Be(20);

            updatedSubStage.Clues.Should().HaveCount(1);
            updatedSubStage.Clues.Single().Id.Should().Be(clueId); // ID preserved
            updatedSubStage.Clues.Single().Text.Should().Be("New Clue");

            updatedSubStage.Question.Id.Should().Be(questionId); // ID preserved
            updatedSubStage.Question.Text.Should().Be("New Question?");
            updatedSubStage.Question.Answers.Should().HaveCount(1);
            updatedSubStage.Question.Answers.Single().Id.Should().Be(answerId); // ID preserved
            updatedSubStage.Question.Answers.Single().Text.Should().Be("New Answer");

            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Remove_SubStage_When_Not_Present_In_Incoming_List()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var stageId = Guid.NewGuid();
            var subStageId1 = Guid.NewGuid();
            var subStageId2 = Guid.NewGuid();

            var subStage1 = TreasureHuntSubStage.Create(subStageId1, "SubStage 1", 1, 10, "Objective", "CODE-1", null);
            var subStage2 = TreasureHuntSubStage.Create(subStageId2, "SubStage 2", 2, 20, "Objective", "CODE-2", null);

            var stage = Stage.Create(stageId, "Stage Name", "Stage Desc", 1, new List<SubStage> { subStage1, subStage2 });
            var mission = Mission.Create(missionId, "Mission", "Desc", DifficultyLevel.Beginner, 60, new List<Stage> { stage });

            // We only include subStage1 in the command, so subStage2 should be removed.
            var command = new UpdateStageCommand(
                missionId,
                stageId,
                "Stage Name",
                "Stage Desc",
                new List<CreateSubStageCommand>
                {
                    new CreateSubStageCommand("SubStage 1", 1, "TreasureHunt", 10, null, "Objective", "CODE-1", null, subStageId1)
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
            stage.SubStages.Should().NotContain(s => s.Id == subStageId2);
            stage.SubStages.Should().Contain(s => s.Id == subStageId1);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Update_TreasureHuntSubStage_InPlace_Successfully()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var stageId = Guid.NewGuid();
            var subStageId = Guid.NewGuid();

            var subStage = TreasureHuntSubStage.Create(subStageId, "Old Title", 1, 10, "Old Objective", "OLD-CODE", null, 10.0, 20.0, 100);
            var stage = Stage.Create(stageId, "Stage Name", "Stage Desc", 1, new List<SubStage> { subStage });
            var mission = Mission.Create(missionId, "Mission", "Desc", DifficultyLevel.Beginner, 60, new List<Stage> { stage });

            var command = new UpdateStageCommand(
                missionId,
                stageId,
                "Stage Name",
                "Stage Desc",
                new List<CreateSubStageCommand>
                {
                    new CreateSubStageCommand(
                        "New Title",
                        1,
                        "TreasureHunt",
                        15,
                        null,
                        "New Objective",
                        "NEW-CODE",
                        null,
                        subStageId,
                        11.0,
                        21.0,
                        150
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
            var updated = stage.SubStages.Single() as TreasureHuntSubStage;
            updated.Should().NotBeNull();
            updated!.Id.Should().Be(subStageId);
            updated.Title.Should().Be("New Title");
            updated.Points.Should().Be(15);
            updated.Objective.Should().Be("New Objective");
            updated.SecretCode.Should().Be("NEW-CODE");
            updated.Latitude.Should().Be(11.0);
            updated.Longitude.Should().Be(21.0);
            updated.SearchRadiusMeters.Should().Be(150);

            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Throw_ArgumentException_When_Updating_TriviaSubStage_Without_Question()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var stageId = Guid.NewGuid();
            var subStageId = Guid.NewGuid();

            var answer = Answer.Create(Guid.NewGuid(), "Answer", true);
            var question = Question.Create(Guid.NewGuid(), "Question?", new List<Answer> { answer });
            var subStage = TriviaSubStage.Create(subStageId, "Old Title", 1, 10, question, null);
            var stage = Stage.Create(stageId, "Stage Name", "Stage Desc", 1, new List<SubStage> { subStage });
            var mission = Mission.Create(missionId, "Mission", "Desc", DifficultyLevel.Beginner, 60, new List<Stage> { stage });

            var command = new UpdateStageCommand(
                missionId,
                stageId,
                "Stage Name",
                "Stage Desc",
                new List<CreateSubStageCommand>
                {
                    new CreateSubStageCommand("New Title", 1, "Trivia", 15, null, null, null, null, subStageId)
                }
            );

            _repositoryMock.Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mission);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*must have a question*");
        }
    }
}
