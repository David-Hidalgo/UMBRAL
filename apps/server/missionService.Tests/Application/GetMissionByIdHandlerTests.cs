using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using MissionService.Application.Handlers;
using MissionService.Application.Queries;
using MissionService.Domain.Entities;
using MissionService.Domain.Enums;
using MissionService.Domain.Exceptions;
using MissionService.Domain.Repositories;
using Xunit;

namespace MissionService.Tests.Application
{
    public class GetMissionByIdHandlerTests
    {
        private readonly Mock<IMissionRepository> _repositoryMock;
        private readonly GetMissionByIdHandler _handler;

        public GetMissionByIdHandlerTests()
        {
            _repositoryMock = new Mock<IMissionRepository>(MockBehavior.Strict);
            _handler = new GetMissionByIdHandler(_repositoryMock.Object);
        }

        [Fact]
        public async Task Handle_Should_Return_MissionDetailDto_When_Mission_Exists()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var mission = CreateMissionWithTriviaSubStage(missionId);
            var query = new GetMissionByIdQuery(missionId);

            _repositoryMock
                .Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mission);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Id.Should().Be(missionId);
            result.Name.Should().Be("Test Mission");
            result.State.Should().Be("Inactive");
            result.DifficultyLevel.Should().Be("Beginner");
            result.MaxTimeMinutes.Should().Be(60);
            result.Stages.Should().HaveCount(1);

            var stage = result.Stages.Single();
            stage.SubStages.Should().HaveCount(1);

            var trivia = stage.SubStages.Single();
            trivia.Question.Should().NotBeNull();
            trivia.Question!.Text.Should().Be("What is 2+2?");
            trivia.Question.Answers.Should().HaveCount(2);
            trivia.Question.Answers.Should().Contain(a => a.IsCorrect);
        }

        [Fact]
        public async Task Handle_Should_Include_TreasureHunt_Fields_When_SubStage_Is_TreasureHunt()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var mission = CreateMissionWithTreasureHuntSubStage(missionId);
            var query = new GetMissionByIdQuery(missionId);

            _repositoryMock
                .Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mission);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            var treasureHunt = result.Stages.Single().SubStages.Single();
            treasureHunt.Objective.Should().Be("Find the hidden keys in the room");
            treasureHunt.SecretCode.Should().Be("SECRET-CODE-123");
            treasureHunt.Clues.Should().NotBeNull();
            treasureHunt.Clues!.Should().HaveCount(1);
            treasureHunt.Clues!.Single().Text.Should().Be("Look under the desk");
        }

        [Fact]
        public async Task Handle_Should_Throw_MissionNotFoundException_When_Mission_Does_Not_Exist()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var query = new GetMissionByIdQuery(missionId);

            _repositoryMock
                .Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Mission?)null);

            // Act
            Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<MissionNotFoundException>();
        }

        [Fact]
        public async Task Handle_Should_Order_Stages_By_Order_Field()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var stageFirst = Stage.Create(Guid.NewGuid(), "First", "First stage", 0, new List<SubStage>());
            var stageSecond = Stage.Create(Guid.NewGuid(), "Second", "Second stage", 2, new List<SubStage>());
            var stageThird = Stage.Create(Guid.NewGuid(), "Third", "Third stage", 1, new List<SubStage>());
            var mission = Mission.Create(
                missionId,
                "Ordered Mission",
                "Description",
                DifficultyLevel.Beginner,
                60,
                new List<Stage> { stageSecond, stageThird, stageFirst });
            var query = new GetMissionByIdQuery(missionId);

            _repositoryMock
                .Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mission);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Stages.Select(s => s.Order).Should().Equal(0, 1, 2);
            result.Stages.Select(s => s.Name).Should().Equal("First", "Third", "Second");
        }

        [Fact]
        public async Task Handle_Should_Order_SubStages_By_Order_Field()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var triviaFirst = CreateTriviaSubStage("First Trivia", 0);
            var triviaSecond = CreateTriviaSubStage("Second Trivia", 2);
            var triviaThird = CreateTriviaSubStage("Third Trivia", 1);
            var stage = Stage.Create(
                Guid.NewGuid(),
                "Stage 1",
                "Description",
                0,
                new List<SubStage> { triviaSecond, triviaThird, triviaFirst });
            var mission = Mission.Create(
                missionId,
                "SubStage Order Mission",
                "Description",
                DifficultyLevel.Beginner,
                60,
                new List<Stage> { stage });
            var query = new GetMissionByIdQuery(missionId);

            _repositoryMock
                .Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mission);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            var subStages = result.Stages.Single().SubStages;
            subStages.Select(ss => ss.Order).Should().Equal(0, 1, 2);
            subStages.Select(ss => ss.Title).Should().Equal("First Trivia", "Third Trivia", "Second Trivia");
        }

        private static Mission CreateMissionWithTriviaSubStage(Guid missionId)
        {
            var trivia = CreateTriviaSubStage("Trivia 1", 0);
            var stage = Stage.Create(Guid.NewGuid(), "Stage 1", "Stage description", 0, new List<SubStage> { trivia });
            return Mission.Create(missionId, "Test Mission", "Mission description", DifficultyLevel.Beginner, 60, new List<Stage> { stage });
        }

        private static Mission CreateMissionWithTreasureHuntSubStage(Guid missionId)
        {
            var clues = new List<Clue>
            {
                Clue.Create(Guid.NewGuid(), "Look under the desk", 0)
            };
            var treasureHunt = TreasureHuntSubStage.Create(
                Guid.NewGuid(),
                "Treasure Hunt",
                0,
                20,
                "Find the hidden keys in the room",
                "SECRET-CODE-123",
                clues);
            var stage = Stage.Create(
                Guid.NewGuid(),
                "Stage 1",
                "Stage description",
                0,
                new List<SubStage> { treasureHunt });
            return Mission.Create(missionId, "Treasure Mission", "Mission description", DifficultyLevel.Beginner, 60, new List<Stage> { stage });
        }

        private static TriviaSubStage CreateTriviaSubStage(string title, int order)
        {
            var answers = new List<Answer>
            {
                Answer.Create(Guid.NewGuid(), "Correct", true),
                Answer.Create(Guid.NewGuid(), "Wrong", false)
            };
            var question = Question.Create(Guid.NewGuid(), "What is 2+2?", answers);
            return TriviaSubStage.Create(Guid.NewGuid(), title, order, 10, question);
        }
    }
}
