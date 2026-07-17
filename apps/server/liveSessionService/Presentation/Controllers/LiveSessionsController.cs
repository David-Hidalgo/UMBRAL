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

namespace LiveSessionService.Presentation.Controllers
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

        // --- Pistas (RF-06 / RF-07) ---

        // El jugador de un equipo solicita una pista para su reto actual.
        [HttpPost("{id:guid}/teams/{teamId:guid}/hint-requests")]
        [Authorize]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RequestHint(
            Guid id,
            Guid teamId,
            [FromBody] RequestHintRequest request,
            CancellationToken cancellationToken)
        {
            var keycloakUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(keycloakUserId))
            {
                return BadRequest("No se pudo identificar al usuario desde el token JWT.");
            }

            var command = new RequestHintCommand(id, teamId, request.SubStageId, keycloakUserId);
            var requestId = await _mediator.Send(command, cancellationToken);
            return Created(string.Empty, requestId);
        }

        // El operador consulta las solicitudes de pista (opcionalmente por estado: Pendiente/Resuelta).
        [HttpGet("{id:guid}/hint-requests")]
        [Authorize(Roles = "admin,operator")]
        [ProducesResponseType(typeof(IReadOnlyList<HintRequestDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetHintRequests(
            Guid id,
            [FromQuery] string? status,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetHintRequestsQuery(id, status), cancellationToken);
            return Ok(result);
        }

        // El operador libera una pista concreta a un equipo.
        [HttpPost("{id:guid}/teams/{teamId:guid}/hints")]
        [Authorize(Roles = "admin,operator")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GrantHint(
            Guid id,
            Guid teamId,
            [FromBody] GrantHintRequest request,
            CancellationToken cancellationToken)
        {
            var jwtToken = HttpContext.Request.Headers.Authorization.ToString();
            var command = new GrantHintCommand(id, teamId, request.SubStageId, request.ClueId, jwtToken);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }

        // El equipo consulta las pistas ya liberadas a él (RF-06 / reconexión).
        [HttpGet("{id:guid}/teams/{teamId:guid}/hints")]
        [Authorize]
        [ProducesResponseType(typeof(IReadOnlyList<ReleasedHintDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetTeamHints(
            Guid id,
            Guid teamId,
            CancellationToken cancellationToken)
        {
            var keycloakUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(keycloakUserId))
            {
                return BadRequest("No se pudo identificar al usuario desde el token JWT.");
            }

            // El operador/admin puede consultar las pistas de cualquier equipo; el jugador solo las suyas.
            var isPrivileged = User.IsInRole("admin") || User.IsInRole("operator");

            var result = await _mediator.Send(
                new GetTeamReleasedHintsQuery(id, teamId, keycloakUserId, isPrivileged), cancellationToken);
            return Ok(result);
        }
    }

    public record CreateLiveSessionRequest(Guid MissionId, string Name, DateTime? ScheduledAt);
    public record TransitionStatusRequest(LiveSessionStatus TargetStatus);
    public record CreateTeamRequest(string TeamName);
    public record AdvanceStageRequest(int NextStageIndex);
    public record RequestHintRequest(Guid SubStageId);
    public record GrantHintRequest(Guid SubStageId, Guid ClueId);
}
