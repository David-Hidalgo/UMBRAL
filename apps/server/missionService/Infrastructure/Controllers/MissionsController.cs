using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MissionService.Application.Commands;
using MissionService.Application.DTOs;

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
                        ss.Clues?.Select(c => new CreateClueCommand(c.Text, c.Order, c.PenaltyPoints)).ToList()
                    )).ToList()
                )).ToList()
            );

            var missionId = await _mediator.Send(command, cancellationToken);
            return Created($"/api/missions/{missionId}", new { id = missionId });
        }
    }
}
