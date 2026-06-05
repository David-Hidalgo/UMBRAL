using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using api.Application.DTOs;
using api.Application.Interfaces;

namespace api.Infrastructure.Controllers;

[ApiController]
[Route("api/missions")]
[Authorize]
public sealed class MissionsController : ControllerBase
{
    private readonly IMissionServiceClient _missionService;

    public MissionsController(IMissionServiceClient missionService)
    {
        _missionService = missionService;
    }

    [HttpGet]
    [Authorize(Roles = "admin,operator")]
    [ProducesResponseType(typeof(IEnumerable<MissionSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var missions = await _missionService.GetMissionsAsync(search, status, cancellationToken);
        return Ok(missions);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "admin,operator")]
    [ProducesResponseType(typeof(MissionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _missionService.GetMissionByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "admin,operator")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Create([FromBody] CreateMissionRequest request, CancellationToken cancellationToken)
    {
        var missionId = await _missionService.CreateMissionAsync(request, cancellationToken);
        return Created($"/api/missions/{missionId}", new { id = missionId });
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "admin,operator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMissionRequest request, CancellationToken cancellationToken)
    {
        await _missionService.UpdateMissionAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin,operator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _missionService.DeleteMissionAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/stages")]
    [Authorize(Roles = "admin,operator")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> AddStage(Guid id, [FromBody] CreateStageRequest request, CancellationToken cancellationToken)
    {
        var stageId = await _missionService.AddStageAsync(id, request, cancellationToken);
        return Created($"/api/missions/{id}/stages/{stageId}", new { id = stageId });
    }

    [HttpPut("{id:guid}/stages/{stageId:guid}")]
    [Authorize(Roles = "admin,operator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> UpdateStage(Guid id, Guid stageId, [FromBody] CreateStageRequest request, CancellationToken cancellationToken)
    {
        await _missionService.UpdateStageAsync(id, stageId, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/stages/{stageId:guid}")]
    [Authorize(Roles = "admin,operator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> DeleteStage(Guid id, Guid stageId, CancellationToken cancellationToken)
    {
        await _missionService.DeleteStageAsync(id, stageId, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/stages/reorder")]
    [Authorize(Roles = "admin,operator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> ReorderStages(Guid id, [FromBody] List<Guid> stageIds, CancellationToken cancellationToken)
    {
        await _missionService.ReorderStagesAsync(id, stageIds, cancellationToken);
        return NoContent();
    }
}

