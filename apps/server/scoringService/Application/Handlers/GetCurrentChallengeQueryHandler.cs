using System;
using System.Collections.Generic;
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
    /// Construye la vista del reto actual para el jugador: toma las etapas activas del
    /// SessionMonitor y pide a missionService (con la identidad de servicio) su contenido
    /// saneado, sin exponer respuestas correctas ni códigos secretos. Si se indica el equipo,
    /// marca qué subetapas ya resolvió para mostrarlas una a una.
    /// </summary>
    public sealed class GetCurrentChallengeQueryHandler
        : IRequestHandler<GetCurrentChallengeQuery, PlayerChallengeDto>
    {
        private readonly ISessionMonitorRepository _sessionMonitorRepository;
        private readonly IMissionContentClient _missionContentClient;
        private readonly IScoringRepository _scoringRepository;

        public GetCurrentChallengeQueryHandler(
            ISessionMonitorRepository sessionMonitorRepository,
            IMissionContentClient missionContentClient,
            IScoringRepository scoringRepository)
        {
            _sessionMonitorRepository = sessionMonitorRepository;
            _missionContentClient = missionContentClient;
            _scoringRepository = scoringRepository;
        }

        public async Task<PlayerChallengeDto> Handle(GetCurrentChallengeQuery request, CancellationToken cancellationToken)
        {
            var monitor = await _sessionMonitorRepository.GetBySessionIdAsync(request.SessionId, cancellationToken)
                ?? throw new SessionMonitorNotFoundException(request.SessionId);

            var subStages = await _missionContentClient.GetPlayerSubStagesAsync(
                monitor.MissionId, monitor.ActiveSubStageIds, cancellationToken);

            if (request.TeamId is Guid teamId)
            {
                var marked = new List<PlayerSubStageDto>(subStages.Count);
                foreach (var subStage in subStages)
                {
                    // Trivia: cerrada tras cualquier intento. Búsqueda del tesoro: cerrada al resolverla.
                    var closed = subStage.Type == SubmissionType.Trivia
                        ? await _scoringRepository.HasAnySubmissionAsync(
                            request.SessionId, teamId, subStage.Id, cancellationToken)
                        : await _scoringRepository.HasSolvedSubStageAsync(
                            request.SessionId, teamId, subStage.Id, cancellationToken);
                    marked.Add(subStage with { Closed = closed });
                }
                subStages = marked;
            }

            return new PlayerChallengeDto(request.SessionId, monitor.Status.ToString(), subStages);
        }
    }
}
