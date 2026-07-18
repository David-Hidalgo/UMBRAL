using System;
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
    public class RequestHintHandler : IRequestHandler<RequestHintCommand, Guid>
    {
        private readonly ILiveSessionRepository _repository;
        private readonly IEventBus _eventBus;
        private readonly ISessionNotificationService _notificationService;

        public RequestHintHandler(
            ILiveSessionRepository repository,
            IEventBus eventBus,
            ISessionNotificationService notificationService)
        {
            _repository = repository;
            _eventBus = eventBus;
            _notificationService = notificationService;
        }

        public async Task<Guid> Handle(RequestHintCommand request, CancellationToken cancellationToken)
        {
            var session = await _repository.GetByIdAsync(request.SessionId, cancellationToken);
            if (session == null)
            {
                throw new BusinessException("La sesión de juego especificada no existe.");
            }

            // El dominio valida estado Activa, pertenencia del jugador al equipo y no duplicar la solicitud.
            var hintRequest = session.RequestHint(request.TeamId, request.SubStageId, request.KeycloakUserId, DateTime.UtcNow);

            await _repository.AddHintRequestAsync(hintRequest, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            // Evento de integración (RabbitMQ)
            await _eventBus.PublishAsync(
                new HintRequestedIntegrationEvent(session.Id, request.TeamId, request.SubStageId, hintRequest.RequestedAt),
                cancellationToken
            );

            // Notificación SignalR al operador (grupo de la sesión)
            var teamName = session.Teams.First(t => t.Id == request.TeamId).Name;
            await _notificationService.NotifyHintRequestedAsync(
                session.Id, hintRequest.Id, request.TeamId, teamName, request.SubStageId, hintRequest.RequestedAt, cancellationToken);

            return hintRequest.Id;
        }
    }
}
