using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MissionService.Domain.Entities;
using MissionService.Domain.Enums;
using MissionService.Infrastructure.Persistence;
using Xunit;

namespace MissionService.Tests.Application
{
    public class MissionRepositoryUpdateStageTests : IDisposable
    {
        private readonly DbContextOptions<MissionDbContext> _options;
        private MissionDbContext _context;
        private MissionRepository _repository;

        public MissionRepositoryUpdateStageTests()
        {
            _options = new DbContextOptionsBuilder<MissionDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new MissionDbContext(_options);
            _repository = new MissionRepository(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task UpdateStage_Should_PersistChanges_And_ReplaceSubStages_Correctly()
        {
            // Arrange: Seed a mission with an initial stage and substage
            var missionId = Guid.NewGuid();
            var stageId = Guid.NewGuid();
            var subStageId1 = Guid.NewGuid();
            var answerId1 = Guid.NewGuid();
            var questionId1 = Guid.NewGuid();

            var initialAnswers = new List<Answer> { Answer.Create(answerId1, "Correct Answer", true) };
            var initialQuestion = Question.Create(questionId1, "Initial Question?", initialAnswers);
            var initialSubStage = TriviaSubStage.Create(
                subStageId1,
                "Initial SubStage",
                1,
                10,
                initialQuestion,
                new List<Clue>()
            );

            var stage = Stage.Create(stageId, "Initial Stage", "Initial Desc", 1, new List<SubStage> { initialSubStage });
            var mission = Mission.Create(missionId, "Mission Alpha", "Description", DifficultyLevel.Beginner, 60, new List<Stage> { stage });

            _context.Missions.Add(mission);
            await _context.SaveChangesAsync();

            // Reset context to clear tracking and simulate a new request
            _context.Dispose();
            _context = new MissionDbContext(_options);
            _repository = new MissionRepository(_context);

            // Act: Load mission via repository, update stage with new substages
            var trackedMission = await _repository.GetByIdAsync(missionId);
            trackedMission.Should().NotBeNull();

            var newAnswerId = Guid.NewGuid();
            var newQuestionId = Guid.NewGuid();
            var newSubStageId = Guid.NewGuid();

            var newAnswers = new List<Answer> { Answer.Create(newAnswerId, "New Correct Answer", true) };
            var newQuestion = Question.Create(newQuestionId, "New Question?", newAnswers);
            var newSubStage = TriviaSubStage.Create(
                newSubStageId,
                "New SubStage",
                1,
                20,
                newQuestion,
                new List<Clue>()
            );

            trackedMission!.UpdateStage(stageId, "Updated Stage Name", "Updated Stage Desc", new List<SubStage> { newSubStage });
            await _repository.SaveChangesAsync();

            // Assert: Verify changes in a new clean context instance
            using var assertContext = new MissionDbContext(_options);
            var assertRepository = new MissionRepository(assertContext);
            var resultMission = await assertRepository.GetByIdAsync(missionId);

            resultMission.Should().NotBeNull();
            var resultStage = resultMission!.Stages.Single(s => s.Id == stageId);
            resultStage.Name.Should().Be("Updated Stage Name");
            resultStage.Description.Should().Be("Updated Stage Desc");

            // Verify substages are updated
            resultStage.SubStages.Should().HaveCount(1);
            var resultSubStage = resultStage.SubStages.Single();
            resultSubStage.Id.Should().Be(newSubStageId);
            resultSubStage.Title.Should().Be("New SubStage");
            resultSubStage.Points.Should().Be(20);

            // Verify the old substage, question, answers are removed/cleaned up (no orphan records left)
            assertContext.SubStages.Any(s => s.Id == subStageId1).Should().BeFalse();
            assertContext.Questions.Any(q => q.Id == questionId1).Should().BeFalse();
            assertContext.Answers.Any(a => a.Id == answerId1).Should().BeFalse();
        }
    }
}
