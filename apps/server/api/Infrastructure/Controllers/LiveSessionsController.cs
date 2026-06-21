using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using api.Application.DTOs;
using api.Application.Interfaces;

namespace api.Infrastructure.Controllers
{
    [ApiController]
    [Route("api/sessions")]
    [Authorize]
    public sealed class LiveSessionsController : ControllerBase
    {
        private readonly ILiveSessionServiceClient _liveSessionService;

        public LiveSessionsController(ILiveSessionServiceClient liveSessionService)
        {
            _liveSessionService = liveSessionService;
        }

        [HttpPost]
        [Authorize(Roles = "admin,operator")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> Create([FromBody] CreateLiveSessionRequest request, CancellationToken cancellationToken)
        {
            var sessionId = await _liveSessionService.CreateSessionAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = sessionId }, sessionId);
        }

        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<LiveSessionSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> GetAll(
            [FromQuery] Guid? missionId,
            [FromQuery] string? status,
            CancellationToken cancellationToken)
        {
            var sessions = await _liveSessionService.GetAllSessionsAsync(missionId, status, cancellationToken);
            return Ok(sessions);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(LiveSessionDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var session = await _liveSessionService.GetSessionByIdAsync(id, cancellationToken);
            return Ok(session);
        }

        [HttpPatch("{id:guid}/status")]
        [Authorize(Roles = "admin,operator")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> TransitionStatus(
            Guid id,
            [FromBody] TransitionStatusRequest request,
            CancellationToken cancellationToken)
        {
            await _liveSessionService.TransitionStatusAsync(id, request, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:guid}/teams")]
        [Authorize(Roles = "admin,operator")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> CreateTeam(
            Guid id,
            [FromBody] CreateTeamRequest request,
            CancellationToken cancellationToken)
        {
            var teamId = await _liveSessionService.CreateTeamAsync(id, request, cancellationToken);
            return Created(string.Empty, teamId);
        }

        [HttpPost("{id:guid}/teams/{teamId:guid}/join")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> JoinTeam(
            Guid id,
            Guid teamId,
            CancellationToken cancellationToken)
        {
            await _liveSessionService.JoinTeamAsync(id, teamId, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:guid}/advance")]
        [Authorize(Roles = "admin,operator")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> AdvanceStage(
            Guid id,
            [FromBody] AdvanceStageRequest request,
            CancellationToken cancellationToken)
        {
            await _liveSessionService.AdvanceStageAsync(id, request, cancellationToken);
            return NoContent();
        }
    }
}
