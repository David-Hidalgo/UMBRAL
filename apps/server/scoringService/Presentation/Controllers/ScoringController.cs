using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ScoringService.Application.Commands;
using ScoringService.Application.DTOs;
using ScoringService.Application.Queries;

namespace ScoringService.Presentation.Controllers
{
    [ApiController]
    [Route("api/scoring/sessions")]
    public sealed class ScoringController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ScoringController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Un equipo envía una respuesta/evidencia para una etapa (RF-08).</summary>
        [HttpPost("{sessionId:guid}/teams/{teamId:guid}/submissions")]
        [Authorize]
        [ProducesResponseType(typeof(SubmitEvidenceResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> SubmitEvidence(
            Guid sessionId, Guid teamId, [FromBody] SubmitEvidenceRequest request, CancellationToken cancellationToken)
        {
            var jwtToken = HttpContext.Request.Headers.Authorization.ToString();
            var command = new SubmitEvidenceCommand(
                sessionId, teamId, request.SubStageId, request.Type, request.Payload, jwtToken);

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        /// <summary>El operador valida manualmente una evidencia pendiente (RF-10).</summary>
        [HttpPatch("{sessionId:guid}/submissions/{submissionId:guid}/validate")]
        [Authorize(Roles = "admin,operator")]
        [ProducesResponseType(typeof(SubmitEvidenceResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ValidateEvidence(
            Guid sessionId, Guid submissionId, [FromBody] ValidateEvidenceRequest request, CancellationToken cancellationToken)
        {
            var jwtToken = HttpContext.Request.Headers.Authorization.ToString();
            var command = new ValidateEvidenceCommand(sessionId, submissionId, request.IsCorrect, GetActor(), jwtToken);

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        /// <summary>El operador aplica una penalización justificada a un equipo (RF-11).</summary>
        [HttpPost("{sessionId:guid}/teams/{teamId:guid}/penalties")]
        [Authorize(Roles = "admin,operator")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ApplyPenalty(
            Guid sessionId, Guid teamId, [FromBody] ApplyPenaltyRequest request, CancellationToken cancellationToken)
        {
            var command = new ApplyPenaltyCommand(sessionId, teamId, request.Points, request.Reason, GetActor());
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }

        /// <summary>Ranking actual de la sesión, ordenado por RB-08 (RF-12).</summary>
        [HttpGet("{sessionId:guid}/ranking")]
        [Authorize]
        [ProducesResponseType(typeof(IReadOnlyList<RankingEntryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRanking(Guid sessionId, CancellationToken cancellationToken)
        {
            var ranking = await _mediator.Send(new GetSessionRankingQuery(sessionId), cancellationToken);
            return Ok(ranking);
        }

        /// <summary>Reto actual de la sesión para el jugador, sin respuestas correctas (RF-06/08).</summary>
        [HttpGet("{sessionId:guid}/current-challenge")]
        [Authorize]
        [ProducesResponseType(typeof(PlayerChallengeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCurrentChallenge(
            Guid sessionId, [FromQuery] Guid? teamId, CancellationToken cancellationToken)
        {
            var challenge = await _mediator.Send(new GetCurrentChallengeQuery(sessionId, teamId), cancellationToken);
            return Ok(challenge);
        }

        /// <summary>Puntaje actual de un equipo, para la app móvil (RF-06).</summary>
        [HttpGet("{sessionId:guid}/teams/{teamId:guid}/score")]
        [Authorize]
        [ProducesResponseType(typeof(TeamScoreDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTeamScore(Guid sessionId, Guid teamId, CancellationToken cancellationToken)
        {
            var score = await _mediator.Send(new GetTeamScoreQuery(sessionId, teamId), cancellationToken);
            return Ok(score);
        }

        /// <summary>Progreso de cada equipo en la etapa actual, para guiar al operador (RF-13).</summary>
        [HttpGet("{sessionId:guid}/stage-progress")]
        [Authorize(Roles = "admin,operator")]
        [ProducesResponseType(typeof(StageProgressDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetStageProgress(Guid sessionId, CancellationToken cancellationToken)
        {
            var progress = await _mediator.Send(new GetStageProgressQuery(sessionId), cancellationToken);
            return Ok(progress);
        }

        /// <summary>Historial de auditoría de la sesión (RF-15).</summary>
        [HttpGet("{sessionId:guid}/events")]
        [Authorize(Roles = "admin,operator")]
        [ProducesResponseType(typeof(IReadOnlyList<SessionEventDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetEvents(Guid sessionId, CancellationToken cancellationToken)
        {
            var events = await _mediator.Send(new GetSessionEventsQuery(sessionId), cancellationToken);
            return Ok(events);
        }

        private string? GetActor()
            => User.FindFirst("preferred_username")?.Value
               ?? User.FindFirst("name")?.Value
               ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value;
    }
}
