using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using LiveSessionService.Application.Commands;
using LiveSessionService.Application.DTOs;
using LiveSessionService.Application.Queries;
using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Infrastructure.Controllers
{
    [ApiController]
    [Route("api/sessions")]
    public sealed class LiveSessionsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public LiveSessionsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [Authorize(Roles = "admin,operator")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Create([FromBody] CreateLiveSessionRequest request, CancellationToken cancellationToken)
        {
            var jwtToken = HttpContext.Request.Headers.Authorization.ToString();
            var command = new CreateLiveSessionCommand(request.MissionId, request.Name, request.ScheduledAt, jwtToken);
            
            var sessionId = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = sessionId }, sessionId);
        }

        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<LiveSessionSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAll(
            [FromQuery] Guid? missionId,
            [FromQuery] string? status,
            CancellationToken cancellationToken)
        {
            var sessions = await _mediator.Send(new GetLiveSessionsQuery(missionId, status), cancellationToken);
            return Ok(sessions);
        }

        [HttpGet("{id:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(LiveSessionDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var session = await _mediator.Send(new GetLiveSessionByIdQuery(id), cancellationToken);
            if (session == null)
            {
                return NotFound();
            }
            return Ok(session);
        }

        [HttpPatch("{id:guid}/status")]
        [Authorize(Roles = "admin,operator")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> TransitionStatus(
            Guid id,
            [FromBody] TransitionStatusRequest request,
            CancellationToken cancellationToken)
        {
            var jwtToken = HttpContext.Request.Headers.Authorization.ToString();
            var command = new TransitionSessionCommand(id, request.TargetStatus, jwtToken);
            
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:guid}/teams")]
        [Authorize(Roles = "admin,operator")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateTeam(
            Guid id,
            [FromBody] CreateTeamRequest request,
            CancellationToken cancellationToken)
        {
            var command = new CreateTeamCommand(id, request.TeamName);
            var teamId = await _mediator.Send(command, cancellationToken);
            return Created(string.Empty, teamId);
        }

        [HttpPost("{id:guid}/teams/{teamId:guid}/join")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> JoinTeam(
            Guid id,
            Guid teamId,
            CancellationToken cancellationToken)
        {
            var keycloakUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            var displayName = User.FindFirst("name")?.Value ?? User.FindFirst("preferred_username")?.Value ?? User.Identity?.Name;

            if (string.IsNullOrEmpty(keycloakUserId) || string.IsNullOrEmpty(displayName))
            {
                return BadRequest("No se pudo identificar al usuario desde el token JWT.");
            }

            var command = new JoinTeamCommand(id, teamId, keycloakUserId, displayName);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:guid}/advance")]
        [Authorize(Roles = "admin,operator")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> AdvanceStage(
            Guid id,
            [FromBody] AdvanceStageRequest request,
            CancellationToken cancellationToken)
        {
            var jwtToken = HttpContext.Request.Headers.Authorization.ToString();
            var command = new AdvanceStageCommand(id, request.NextStageIndex, jwtToken);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }
    }

    public record CreateLiveSessionRequest(Guid MissionId, string Name, DateTime? ScheduledAt);
    public record TransitionStatusRequest(LiveSessionStatus TargetStatus);
    public record CreateTeamRequest(string TeamName);
    public record AdvanceStageRequest(int NextStageIndex);
}
