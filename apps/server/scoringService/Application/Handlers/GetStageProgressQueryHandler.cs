using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ScoringService.Application.Abstractions;
using ScoringService.Application.DTOs;
using ScoringService.Application.Queries;
using ScoringService.Domain.Enums;
using ScoringService.Domain.Exceptions;
using ScoringService.Domain.Repositories;

namespace ScoringService.Application.Handlers
{
    /// <summary>
    /// Calcula cuántas subetapas de la etapa actual ha "cerrado" cada equipo (Trivia = respondida,
    /// Búsqueda = resuelta) y si todos terminaron, para que el operador decida cuándo avanzar.
    /// </summary>
    public sealed class GetStageProgressQueryHandler
        : IRequestHandler<GetStageProgressQuery, StageProgressDto>
    {
        private readonly ISessionMonitorRepository _sessionMonitorRepository;
        private readonly IMissionContentClient _missionContentClient;
        private readonly IScoringRepository _scoringRepository;

        public GetStageProgressQueryHandler(
            ISessionMonitorRepository sessionMonitorRepository,
            IMissionContentClient missionContentClient,
            IScoringRepository scoringRepository)
        {
            _sessionMonitorRepository = sessionMonitorRepository;
            _missionContentClient = missionContentClient;
            _scoringRepository = scoringRepository;
        }

        public async Task<StageProgressDto> Handle(GetStageProgressQuery request, CancellationToken cancellationToken)
        {
            var monitor = await _sessionMonitorRepository.GetBySessionIdAsync(request.SessionId, cancellationToken)
                ?? throw new SessionMonitorNotFoundException(request.SessionId);

            var subStages = await _missionContentClient.GetPlayerSubStagesAsync(
                monitor.MissionId, monitor.ActiveSubStageIds, cancellationToken);
            var subStageCount = subStages.Count;

            var teams = new List<TeamStageProgressDto>();
            foreach (var team in monitor.Teams)
            {
                var completed = 0;
                foreach (var subStage in subStages)
                {
                    // Trivia: cerrada tras cualquier intento. Búsqueda: cerrada al resolverla.
                    var closed = subStage.Type == SubmissionType.Trivia
                        ? await _scoringRepository.HasAnySubmissionAsync(request.SessionId, team.TeamId, subStage.Id, cancellationToken)
                        : await _scoringRepository.HasSolvedSubStageAsync(request.SessionId, team.TeamId, subStage.Id, cancellationToken);
                    if (closed)
                        completed++;
                }
                teams.Add(new TeamStageProgressDto(team.TeamId, team.Code.Value, completed));
            }

            var allComplete = subStageCount > 0 && teams.Count > 0 && teams.All(t => t.Completed == subStageCount);
            return new StageProgressDto(subStageCount, teams, allComplete);
        }
    }
}
