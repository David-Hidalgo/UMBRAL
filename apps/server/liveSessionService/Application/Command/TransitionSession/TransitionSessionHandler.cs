using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using LiveSessionService.Application.Commands;
using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.IntegrationEvents;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.Repositories;

namespace LiveSessionService.Application.Handlers
{
    public class TransitionSessionHandler : IRequestHandler<TransitionSessionCommand, Unit>
    {
        private readonly ILiveSessionRepository _repository;
        private readonly IMissionServiceClient _missionClient;
        private readonly IEventBus _eventBus;
        private readonly ISessionNotificationService _notificationService;

        public TransitionSessionHandler(
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

        public async Task<Unit> Handle(TransitionSessionCommand request, CancellationToken cancellationToken)
        {
            var session = await _repository.GetByIdAsync(request.SessionId, cancellationToken);
            if (session == null)
            {
                throw new BusinessException("La sesión de juego especificada no existe.");
            }

            var previousStatus = session.Status;
            var targetStatus = request.TargetStatus;

            // Si ya está en el estado destino, no hacer nada
            if (previousStatus == targetStatus)
            {
                return Unit.Value;
            }

            var now = DateTime.UtcNow;

            // Aplicar transición según el patrón de estado. Se pasa `now` a las transiciones
            // con temporizador (activar arranca el reloj, pausar lo congela, reanudar lo restaura).
            if (targetStatus == LiveSessionStatus.Preparacion)
            {
                session.StartPreparation();
            }
            else if (targetStatus == LiveSessionStatus.Activa)
            {
                if (previousStatus == LiveSessionStatus.Pausada)
                {
                    session.Resume(now);
                }
                else
                {
                    session.Activate(now);
                }
            }
            else if (targetStatus == LiveSessionStatus.Pausada)
            {
                session.Pause(now);
            }
            else if (targetStatus == LiveSessionStatus.Finalizada)
            {
                session.Finish();
            }
            else if (targetStatus == LiveSessionStatus.Cancelada)
            {
                session.Cancel();
            }
            else
            {
                throw new BusinessException($"Transición no soportada al estado: {targetStatus}");
            }

            await _repository.SaveChangesAsync(cancellationToken);

            // Publicar eventos de integración específicos
            if (targetStatus == LiveSessionStatus.Activa && previousStatus != LiveSessionStatus.Pausada)
            {
                // Al activar por primera vez, obtener los substages de la primera etapa (índice 0)
                var subStageIds = await GetSubStageIdsForStageAsync(session.MissionId, 0, request.JwtToken, cancellationToken);
                await _eventBus.PublishAsync(new SessionActivatedIntegrationEvent(session.Id, subStageIds), cancellationToken);
            }
            else if (targetStatus == LiveSessionStatus.Pausada)
            {
                await _eventBus.PublishAsync(new SessionPausedIntegrationEvent(session.Id), cancellationToken);
            }
            else if (targetStatus == LiveSessionStatus.Finalizada)
            {
                // Decrementar el count de la etapa activa actual
                var subStageIds = await GetSubStageIdsForStageAsync(session.MissionId, session.CurrentStageIndex, request.JwtToken, cancellationToken);
                await _eventBus.PublishAsync(new SessionFinishedIntegrationEvent(session.Id, subStageIds), cancellationToken);
            }
            else if (targetStatus == LiveSessionStatus.Cancelada)
            {
                // Si la sesión estaba activa o pausada, decrementar el count de la etapa actual
                var subStageIds = new List<Guid>();
                if (previousStatus == LiveSessionStatus.Activa || previousStatus == LiveSessionStatus.Pausada)
                {
                    subStageIds = await GetSubStageIdsForStageAsync(session.MissionId, session.CurrentStageIndex, request.JwtToken, cancellationToken);
                }
                await _eventBus.PublishAsync(new SessionCancelledIntegrationEvent(session.Id, subStageIds), cancellationToken);
            }

            // Notificación en tiempo real de cambio de estado (incluye el temporizador RF-06).
            await _notificationService.NotifyStateChangedAsync(
                session.Id, session.Status.ToString(), session.EndsAt, session.RemainingSecondsOnPause, cancellationToken);

            return Unit.Value;
        }

        private async Task<List<Guid>> GetSubStageIdsForStageAsync(Guid missionId, int stageIndex, string jwtToken, CancellationToken cancellationToken)
        {
            var mission = await _missionClient.GetMissionByIdAsync(missionId, jwtToken, cancellationToken);
            if (mission == null)
            {
                throw new BusinessException("No se pudo obtener la estructura de la misión.");
            }

            // El índice de etapa es base 0 (0 = primera); el Order de la misión es base 1.
            // Se resuelve por POSICIÓN dentro de las etapas ordenadas por Order, para no
            // confundir el índice 1 con la etapa Order=1 (que es la primera).
            var stage = mission.Stages.OrderBy(s => s.Order).ElementAtOrDefault(stageIndex);
            if (stage == null)
            {
                throw new BusinessException($"No se encontró la etapa con índice {stageIndex} en la misión.");
            }

            return stage.SubStages.Select(ss => ss.Id).ToList();
        }
    }
}
