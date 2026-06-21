using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using LiveSessionService.Application.Commands;
using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.IntegrationEvents;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.Repositories;

namespace LiveSessionService.Application.Handlers
{
    public class AdvanceStageHandler : IRequestHandler<AdvanceStageCommand, Unit>
    {
        private readonly ILiveSessionRepository _repository;
        private readonly IMissionServiceClient _missionClient;
        private readonly IEventBus _eventBus;
        private readonly ISessionNotificationService _notificationService;

        public AdvanceStageHandler(
            ILiveSessionRepository repository,
            IMissionServiceClient missionClient,
            IEventBus eventBus,
            ISessionNotificationService notificationService)
        {
            _repository = repository;
            _missionClient = missionClient;
            _eventBus = eventBus;
            _notificationService = notificationService;
        }

        public async Task<Unit> Handle(AdvanceStageCommand request, CancellationToken cancellationToken)
        {
            var session = await _repository.GetByIdAsync(request.SessionId, cancellationToken);
            if (session == null)
            {
                throw new BusinessException("La sesión de juego especificada no existe.");
            }

            var previousStageIndex = session.CurrentStageIndex;
            var nextStageIndex = request.NextStageIndex;

            session.AdvanceStage(nextStageIndex);

            await _repository.SaveChangesAsync(cancellationToken);

            // Obtener substages de la etapa previa y la nueva etapa para actualizar conteos en el microservicio de misiones
            var previousSubStageIds = await GetSubStageIdsForStageAsync(session.MissionId, previousStageIndex, request.JwtToken, cancellationToken);
            var newSubStageIds = await GetSubStageIdsForStageAsync(session.MissionId, nextStageIndex, request.JwtToken, cancellationToken);

            await _eventBus.PublishAsync(
                new StageTransitionedIntegrationEvent(session.Id, previousSubStageIds, newSubStageIds),
                cancellationToken
            );

            // Notificación SignalR
            await _notificationService.NotifyStageAdvancedAsync(session.Id, session.CurrentStageIndex, cancellationToken);

            return Unit.Value;
        }

        private async Task<List<Guid>> GetSubStageIdsForStageAsync(Guid missionId, int stageIndex, string jwtToken, CancellationToken cancellationToken)
        {
            try
            {
                var mission = await _missionClient.GetMissionByIdAsync(missionId, jwtToken, cancellationToken);
                if (mission == null) return new List<Guid>();

                var stage = mission.Stages.FirstOrDefault(s => s.Order == stageIndex);
                if (stage == null)
                {
                    if (stageIndex >= 0 && stageIndex < mission.Stages.Count)
                    {
                        stage = mission.Stages[stageIndex];
                    }
                }

                return stage?.SubStages.Select(ss => ss.Id).ToList() ?? new List<Guid>();
            }
            catch
            {
                // Devolver lista vacía si hay algún problema consultando la estructura
                return new List<Guid>();
            }
        }
    }
}
