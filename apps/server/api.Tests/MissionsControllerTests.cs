using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using api.Application.DTOs;
using api.Application.Interfaces;
using api.Infrastructure.Controllers;
using Xunit;

namespace api.Tests;

public class MissionsControllerTests
{
    private readonly Mock<IMissionServiceClient> _clientMock = new(MockBehavior.Strict);

    private MissionsController BuildController()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Role, "admin") }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        return new MissionsController(_clientMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };
    }

    private static MissionSummaryDto SampleSummary() =>
        new(Guid.NewGuid(), "Mission Alpha", "Desc", "Draft", 0, "Beginner", 60, 2);

    private static MissionDetailDto SampleDetail(Guid id) =>
        new(id, "Mission Alpha", "Desc", "Draft", 0, "Beginner", 60,
            new List<StageDetailDto>());

    // ─── GetAll ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithMissionList()
    {
        // Arrange
        var missions = new List<MissionSummaryDto> { SampleSummary(), SampleSummary() };
        _clientMock
            .Setup(c => c.GetMissionsAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(missions);
        var controller = BuildController();

        // Act
        var result = await controller.GetAll(null, null, CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(missions);
    }

    [Fact]
    public async Task GetAll_PassesSearchAndStatusToClient()
    {
        // Arrange
        _clientMock
            .Setup(c => c.GetMissionsAsync("alpha", "active", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MissionSummaryDto>());
        var controller = BuildController();

        // Act
        await controller.GetAll("alpha", "active", CancellationToken.None);

        // Assert
        _clientMock.Verify(
            c => c.GetMissionsAsync("alpha", "active", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ─── GetById ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ReturnsOkWithDetail()
    {
        // Arrange
        var id = Guid.NewGuid();
        var detail = SampleDetail(id);
        _clientMock
            .Setup(c => c.GetMissionByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);
        var controller = BuildController();

        // Act
        var result = await controller.GetById(id, CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(detail);
    }

    // ─── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ReturnsCreatedWithMissionId()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var request = new CreateMissionRequest("Mission Alpha", "Desc", "Beginner", 60,
            new List<CreateStageRequest>());
        _clientMock
            .Setup(c => c.CreateMissionAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(missionId);
        var controller = BuildController();

        // Act
        var result = await controller.Create(request, CancellationToken.None);

        // Assert
        var created = result.Should().BeOfType<CreatedResult>().Subject;
        created.Location.Should().Be($"/api/missions/{missionId}");
        created.Value.Should().BeEquivalentTo(new { id = missionId });
    }

    // ─── Update ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ReturnsNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateMissionRequest("New Name", null, null, null, null);
        _clientMock
            .Setup(c => c.UpdateMissionAsync(id, request, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var controller = BuildController();

        // Act
        var result = await controller.Update(id, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    // ─── Delete ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();
        _clientMock
            .Setup(c => c.DeleteMissionAsync(id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var controller = BuildController();

        // Act
        var result = await controller.Delete(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    // ─── AddStage ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddStage_ReturnsCreatedWithStageId()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var request = new CreateStageRequest("Stage 1", "Desc", 1, new List<CreateSubStageRequest>());
        _clientMock
            .Setup(c => c.AddStageAsync(missionId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stageId);
        var controller = BuildController();

        // Act
        var result = await controller.AddStage(missionId, request, CancellationToken.None);

        // Assert
        var created = result.Should().BeOfType<CreatedResult>().Subject;
        created.Location.Should().Be($"/api/missions/{missionId}/stages/{stageId}");
        created.Value.Should().BeEquivalentTo(new { id = stageId });
    }

    // ─── UpdateStage ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStage_ReturnsNoContent()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var request = new CreateStageRequest("Stage Updated", "Desc", 1, new List<CreateSubStageRequest>());
        _clientMock
            .Setup(c => c.UpdateStageAsync(missionId, stageId, request, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var controller = BuildController();

        // Act
        var result = await controller.UpdateStage(missionId, stageId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    // ─── DeleteStage ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteStage_ReturnsNoContent()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        _clientMock
            .Setup(c => c.DeleteStageAsync(missionId, stageId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var controller = BuildController();

        // Act
        var result = await controller.DeleteStage(missionId, stageId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    // ─── ReorderStages ────────────────────────────────────────────────────────

    [Fact]
    public async Task ReorderStages_ReturnsNoContent()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var stageIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        _clientMock
            .Setup(c => c.ReorderStagesAsync(missionId, stageIds, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var controller = BuildController();

        // Act
        var result = await controller.ReorderStages(missionId, stageIds, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }
}
