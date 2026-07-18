using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MissionService.Application.DTOs;
using MissionService.Application.Queries;
using MissionService.Domain.Entities;
using MissionService.Domain.Exceptions;
using MissionService.Domain.Repositories;

namespace MissionService.Application.Handlers
{
    public sealed class GetMissionByIdHandler : IRequestHandler<GetMissionByIdQuery, MissionDetailDto>
    {
        private readonly IMissionRepository _repository;

        public GetMissionByIdHandler(IMissionRepository repository)
        {
            _repository = repository;
        }

        public async Task<MissionDetailDto> Handle(GetMissionByIdQuery request, CancellationToken cancellationToken)
        {
            var mission = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (mission is null)
                throw new MissionNotFoundException(request.Id);

            return MapToDto(mission);
        }

        private static MissionDetailDto MapToDto(Mission mission) =>
            new(
                mission.Id,
                mission.Name,
                mission.Description,
                mission.State.ToString(),
                mission.Stages.SelectMany(s => s.SubStages).Sum(ss => ss.Status.ActiveSessionCount),
                mission.DifficultyLevel.ToString(),
                mission.MaxTimeMinutes,
                mission.Stages
                    .OrderBy(s => s.Order)
                    .Select(MapStage)
                    .ToList()
                    .AsReadOnly()
            );

        private static StageDetailDto MapStage(Stage stage) =>
            new(
                stage.Id,
                stage.Name,
                stage.Description,
                stage.Order,
                stage.SubStages.Sum(ss => ss.Status.ActiveSessionCount),
                stage.SubStages
                    .OrderBy(ss => ss.Order)
                    .Select(MapSubStage)
                    .ToList()
                    .AsReadOnly()
            );

        private static SubStageDetailDto MapSubStage(SubStage ss) => ss switch
        {
            TriviaSubStage t => new(
                t.Id, t.Title, t.Order, t.Points,
                t.Status.ActiveSessionCount,
                new QuestionDetailDto(
                    t.Question.Id,
                    t.Question.Text,
                    t.Question.Answers
                        .Select(a => new AnswerDetailDto(a.Id, a.Text, a.IsCorrect))
                        .ToList()
                        .AsReadOnly()
                ),
                null, null,
                t.Clues
                    .OrderBy(c => c.Order)
                    .Select(c => new ClueDetailDto(c.Id, c.Text, c.Order))
                    .ToList()
                    .AsReadOnly()
            ),
            TreasureHuntSubStage th => new(
                th.Id, th.Title, th.Order, th.Points,
                th.Status.ActiveSessionCount,
                null,
                th.Objective,
                th.SecretCode,
                th.Clues
                    .OrderBy(c => c.Order)
                    .Select(c => new ClueDetailDto(c.Id, c.Text, c.Order))
                    .ToList()
                    .AsReadOnly(),
                th.Latitude, th.Longitude, th.SearchRadiusMeters
            ),
            _ => throw new InvalidOperationException($"Unknown SubStage type: {ss.GetType().Name}")
        };
    }
}
