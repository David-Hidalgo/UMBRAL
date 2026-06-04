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
}

