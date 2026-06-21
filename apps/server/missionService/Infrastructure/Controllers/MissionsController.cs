using System;
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
                request.DifficultyLevel,
                request.MaxTimeMinutes,
                (request.Stages ?? []).Select(s => new CreateStageCommand(
                    s.Name, s.Description, s.Order,
                    (s.SubStages ?? []).Select(ss => new CreateSubStageCommand(
                        ss.Title, ss.Order, ss.Type, ss.Points,
                        ss.Question != null
                            ? new CreateQuestionCommand(
                                ss.Question.Text,
                                (ss.Question.Answers ?? []).Select(a => new CreateAnswerCommand(a.Text, a.IsCorrect, a.Id)).ToList(),
                                ss.Question.Id
                            ) : null,
                        ss.Objective, ss.SecretCode,
                        ss.Clues?.Select(c => new CreateClueCommand(c.Text, c.Order, c.Id)).ToList(),
                        ss.Id
                    )).ToList()
                )).ToList()
            );

            var missionId = await _mediator.Send(command, cancellationToken);
            return Created($"/api/missions/{missionId}", new { id = missionId });
        }

        [HttpPatch("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMissionRequest request, CancellationToken cancellationToken)
        {
            var command = new UpdateMissionCommand(
                id,
                request.Name,
                request.Description,
                request.State,
                request.DifficultyLevel,
                request.MaxTimeMinutes
            );

            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteMissionCommand(id), cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:guid}/stages")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> AddStage(Guid id, [FromBody] CreateStageRequest request, CancellationToken cancellationToken)
        {
            var command = new AddStageCommand(
                id,
                request.Name,
                request.Description,
                request.Order,
                (request.SubStages ?? []).Select(ss => new CreateSubStageCommand(
                    ss.Title, ss.Order, ss.Type, ss.Points,
                    ss.Question != null
                        ? new CreateQuestionCommand(
                            ss.Question.Text,
                            (ss.Question.Answers ?? []).Select(a => new CreateAnswerCommand(a.Text, a.IsCorrect, a.Id)).ToList(),
                            ss.Question.Id
                        ) : null,
                    ss.Objective, ss.SecretCode,
                    ss.Clues?.Select(c => new CreateClueCommand(c.Text, c.Order, c.Id)).ToList(),
                    ss.Id
                )).ToList()
            );

            var stageId = await _mediator.Send(command, cancellationToken);
            return Created($"/api/missions/{id}/stages/{stageId}", new { id = stageId });
        }

        [HttpPut("{id:guid}/stages/{stageId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateStage(Guid id, Guid stageId, [FromBody] CreateStageRequest request, CancellationToken cancellationToken)
        {
            var command = new UpdateStageCommand(
                id,
                stageId,
                request.Name,
                request.Description,
                (request.SubStages ?? []).Select(ss => new CreateSubStageCommand(
                    ss.Title, ss.Order, ss.Type, ss.Points,
                    ss.Question != null
                        ? new CreateQuestionCommand(
                            ss.Question.Text,
                            (ss.Question.Answers ?? []).Select(a => new CreateAnswerCommand(a.Text, a.IsCorrect, a.Id)).ToList(),
                            ss.Question.Id
                        ) : null,
                    ss.Objective, ss.SecretCode,
                    ss.Clues?.Select(c => new CreateClueCommand(c.Text, c.Order, c.Id)).ToList(),
                    ss.Id
                )).ToList()
            );

            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }

        [HttpDelete("{id:guid}/stages/{stageId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DeleteStage(Guid id, Guid stageId, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteStageCommand(id, stageId), cancellationToken);
            return NoContent();
        }

        [HttpPut("{id:guid}/stages/reorder")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ReorderStages(Guid id, [FromBody] List<Guid> stageIds, CancellationToken cancellationToken)
        {
            await _mediator.Send(new ReorderStagesCommand(id, stageIds), cancellationToken);
            return NoContent();
        }
    }
}
