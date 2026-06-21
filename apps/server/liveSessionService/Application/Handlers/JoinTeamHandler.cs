using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using LiveSessionService.Application.Commands;
using LiveSessionService.Application.Interfaces;
using LiveSessionService.Application.IntegrationEvents;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.RepositoryInterfaces;

namespace LiveSessionService.Application.Handlers
{
    public class JoinTeamHandler : IRequestHandler<JoinTeamCommand, Unit>
    {
        private readonly ILiveSessionRepository _repository;
        private readonly IEventBus _eventBus;
        private readonly ISessionNotificationService _notificationService;

        public JoinTeamHandler(
            ILiveSessionRepository repository,
            IEventBus eventBus,
            ISessionNotificationService notificationService)
        {
            _repository = repository;
            _eventBus = eventBus;
            _notificationService = notificationService;
        }

        public async Task<Unit> Handle(JoinTeamCommand request, CancellationToken cancellationToken)
        {
            var session = await _repository.GetByIdAsync(request.SessionId, cancellationToken);
            if (session == null)
            {
                throw new BusinessException("La sesión de juego especificada no existe.");
            }

            var player = Player.Create(
                Guid.NewGuid(),
                request.KeycloakUserId,
                request.DisplayName,
                request.TeamId
            );

            session.AddPlayerToTeam(request.TeamId, player);

            await _repository.AddPlayerAsync(player, cancellationToken);

            await _repository.SaveChangesAsync(cancellationToken);

            // Evento de integración (RabbitMQ)
            await _eventBus.PublishAsync(
                new PlayerJoinedTeamIntegrationEvent(session.Id, request.TeamId, player.KeycloakUserId, player.DisplayName),
                cancellationToken
            );

            // Notificación en tiempo real (SignalR)
            await _notificationService.NotifyPlayerJoinedAsync(session.Id, request.TeamId, player.DisplayName, cancellationToken);

            return Unit.Value;
        }
    }
}
