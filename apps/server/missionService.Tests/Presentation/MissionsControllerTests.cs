using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MissionService.Application.Commands;
using MissionService.Application.DTOs;
using MissionService.Application.Queries;
using MissionService.Presentation.Controllers;
using Xunit;

namespace MissionService.Tests.Presentation
{
    public class MissionsControllerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly MissionsController _controller;

        public MissionsControllerTests()
        {
            _mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
            _controller = new MissionsController(_mediatorMock.Object);
        }

        [Fact]
        public async Task GetAll_ReturnsOkWithMissions()
        {
            // Arrange
            var expectedMissions = new List<MissionSummaryDto>
            {
                new MissionSummaryDto(Guid.NewGuid(), "Mission 1", "Desc 1", "Draft", 0, "Beginner", 60, 1)
            };

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetMissionsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedMissions);

            // Act
            var result = await _controller.GetAll("search", "Draft", CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(expectedMissions);
        }

        [Fact]
        public async Task GetById_ReturnsOkWithMission()
        {
            // Arrange
            var id = Guid.NewGuid();
            var expectedMission = new MissionDetailDto(id, "Mission Detail", "Desc", "Draft", 0, "Beginner", 60, new List<StageDetailDto>());

            _mediatorMock.Setup(m => m.Send(It.Is<GetMissionByIdQuery>(q => q.Id == id), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedMission);

            // Act
            var result = await _controller.GetById(id, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(expectedMission);
        }

        [Fact]
        public async Task Create_ReturnsCreatedResult()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var answers = new List<CreateAnswerRequest> { new CreateAnswerRequest(null, "A", true) };
            var question = new CreateQuestionRequest(null, "Q?", answers);
            var subStages = new List<CreateSubStageRequest>
            {
                new CreateSubStageRequest(null, "SubStage 1", 1, "Trivia", 10, question, null, null, null)
            };
            var stages = new List<CreateStageRequest>
            {
                new CreateStageRequest("Stage 1", "Stage Desc", 1, subStages)
            };
            var request = new CreateMissionRequest("New Mission", "Desc", "Beginner", 60, stages);

            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateMissionCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(missionId);

            // Act
            var result = await _controller.Create(request, CancellationToken.None);

            // Assert
            var createdResult = result.Should().BeOfType<CreatedResult>().Subject;
            createdResult.Location.Should().Be($"/api/missions/{missionId}");
        }

        [Fact]
        public async Task Update_ReturnsNoContent()
        {
            // Arrange
            var id = Guid.NewGuid();
            var request = new UpdateMissionRequest("Updated Name", "Updated Desc", "Active", "Intermediate", 90);

            _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateMissionCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            // Act
            var result = await _controller.Update(id, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Delete_ReturnsNoContent()
        {
            // Arrange
            var id = Guid.NewGuid();

            _mediatorMock.Setup(m => m.Send(It.Is<DeleteMissionCommand>(c => c.Id == id), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            // Act
            var result = await _controller.Delete(id, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task AddStage_ReturnsCreatedResult()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var stageId = Guid.NewGuid();
            var clues = new List<CreateClueRequest> { new CreateClueRequest(null, "Clue", 1) };
            var subStages = new List<CreateSubStageRequest>
            {
                new CreateSubStageRequest(null, "Treasure Hunt", 1, "TreasureHunt", 15, null, "Obj", "SECRET", clues)
            };
            var request = new CreateStageRequest("Stage 2", "Desc", 2, subStages);

            _mediatorMock.Setup(m => m.Send(It.IsAny<AddStageCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(stageId);

            // Act
            var result = await _controller.AddStage(missionId, request, CancellationToken.None);

            // Assert
            var createdResult = result.Should().BeOfType<CreatedResult>().Subject;
            createdResult.Location.Should().Be($"/api/missions/{missionId}/stages/{stageId}");
        }

        [Fact]
        public async Task UpdateStage_ReturnsNoContent()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var stageId = Guid.NewGuid();
            var request = new CreateStageRequest("Updated Stage", "Desc", 1, new List<CreateSubStageRequest>());

            _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateStageCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            // Act
            var result = await _controller.UpdateStage(missionId, stageId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task DeleteStage_ReturnsNoContent()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var stageId = Guid.NewGuid();

            _mediatorMock.Setup(m => m.Send(It.Is<DeleteStageCommand>(c => c.MissionId == missionId && c.StageId == stageId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            // Act
            var result = await _controller.DeleteStage(missionId, stageId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task ReorderStages_ReturnsNoContent()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var stageIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

            _mediatorMock.Setup(m => m.Send(It.Is<ReorderStagesCommand>(c => c.MissionId == missionId && c.StageIdsInOrder == stageIds), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            // Act
            var result = await _controller.ReorderStages(missionId, stageIds, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }
    }
}
