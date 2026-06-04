using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MissionService.Application.Commands;
using MissionService.Application.DTOs;
using MissionService.Application.Queries;

namespace MissionService.Infrastructure.Controllers
{
    [ApiController]
    [Route("api/missions")]
    [Authorize(Roles = "admin,operator")]
    public sealed class MissionsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public MissionsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [Authorize(Roles = "admin,operator")]
        [ProducesResponseType(typeof(IEnumerable<MissionSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? status,
            CancellationToken cancellationToken)
        {
            var missions = await _mediator.Send(new GetMissionsQuery(search, status), cancellationToken);
            return Ok(missions);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(MissionDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMissionByIdQuery(id), cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Create([FromBody] CreateMissionRequest request, CancellationToken cancellationToken)
        {
            var command = new CreateMissionCommand(
                request.Name,
                request.Description,
                (request.Stages ?? []).Select(s => new CreateStageCommand(
                    s.Name,
                    s.Description,
                    s.Order,
                    (s.SubStages ?? []).Select(ss => new CreateSubStageCommand(
                        ss.Title,
                        ss.Order,
                        ss.Type,
                        ss.Points,
                        ss.Question != null
                            ? new CreateQuestionCommand(
                                ss.Question.Text,
                                (ss.Question.Answers ?? []).Select(a => new CreateAnswerCommand(a.Text, a.IsCorrect)).ToList()
                            )
                            : null,
                        ss.Objective,
                        ss.SecretCode,
                        ss.Clues?.Select(c => new CreateClueCommand(c.Text, c.Order)).ToList()
                    )).ToList()
                )).ToList()
            );

            var missionId = await _mediator.Send(command, cancellationToken);
            return Created($"/api/missions/{missionId}", new { id = missionId });
        }
    }
}
