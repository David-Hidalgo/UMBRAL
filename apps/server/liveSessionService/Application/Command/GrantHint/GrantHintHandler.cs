using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LiveSessionService.Application.Commands;
using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.IntegrationEvents;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.Repositories;

namespace LiveSessionService.Application.Handlers
{
    public class GrantHintHandler : IRequestHandler<GrantHintCommand, Unit>
    {
        private readonly ILiveSessionRepository _repository;
        private readonly IMissionServiceClient _missionClient;
        private readonly IEventBus _eventBus;
        private readonly ISessionNotificationService _notificationService;

        public GrantHintHandler(
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

        public async Task<Unit> Handle(GrantHintCommand request, CancellationToken cancellationToken)
        {
            var session = await _repository.GetByIdAsync(request.SessionId, cancellationToken);
            if (session == null)
            {
                throw new BusinessException("La sesión de juego especificada no existe.");
            }

            // El contenido de las pistas vive en el missionService. Se lee con el JWT del
            // operador (rol admin/operator), que sí tiene acceso al detalle de la misión.
            var mission = await _missionClient.GetMissionByIdAsync(session.MissionId, request.JwtToken, cancellationToken);
            if (mission == null)
            {
                throw new BusinessException("No se pudo obtener la estructura de la misión.");
            }

            // El índice de etapa es base 0; el Order de la misión es base 1. Se resuelve por posición.
            var stage = mission.Stages.OrderBy(s => s.Order).ElementAtOrDefault(session.CurrentStageIndex);
            if (stage == null)
            {
                throw new BusinessException($"No se encontró la etapa con índice {session.CurrentStageIndex} en la misión.");
            }

            var subStage = stage.SubStages.FirstOrDefault(ss => ss.Id == request.SubStageId);
            if (subStage == null)
            {
                throw new BusinessException("La subetapa indicada no pertenece a la etapa actual de la partida.");
            }

            // Una subetapa puede no tener ninguna pista configurada (colección vacía). Si no existe la
            // pista solicitada, se rechaza en vez de otorgar contenido inexistente.
            var clue = subStage.Clues?.FirstOrDefault(c => c.Id == request.ClueId);
            if (clue == null)
            {
                throw new BusinessException("La pista indicada no existe para esta subetapa.");
            }

            // El dominio valida estado Activa, pertenencia del equipo y RB-04 (no repetir pista al equipo).
            var released = session.GrantHint(request.TeamId, request.SubStageId, clue.Id, clue.Text, clue.Order, DateTime.UtcNow);

            await _repository.AddReleasedHintAsync(released, cancellationToken);
            try
            {
                await _repository.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Segundo candado de RB-04: bajo concurrencia (dos "otorgar" a la vez), ambos pasan
                // el chequeo en memoria y es el índice único (LiveSessionId, TeamId, ClueId) el que
                // rechaza el duplicado. Se traduce a un mensaje de negocio en vez de un 500.
                throw new BusinessException("Esta pista ya fue liberada a este equipo.");
            }

            // Evento de integración (RabbitMQ)
            await _eventBus.PublishAsync(
                new HintReleasedIntegrationEvent(session.Id, request.TeamId, request.SubStageId, clue.Id, released.ReleasedAt),
                cancellationToken
            );

            // Notificación SignalR: SOLO al equipo destinatario, y aviso al operador de que se resolvió.
            await _notificationService.NotifyHintReleasedAsync(
                session.Id, request.TeamId, released.Id, request.SubStageId, clue.Id, clue.Text, clue.Order, released.ReleasedAt, cancellationToken);
            await _notificationService.NotifyHintRequestResolvedAsync(
                session.Id, request.TeamId, request.SubStageId, cancellationToken);

            return Unit.Value;
        }
    }
}
