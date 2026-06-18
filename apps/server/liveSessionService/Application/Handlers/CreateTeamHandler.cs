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
    public class CreateTeamHandler : IRequestHandler<CreateTeamCommand, Guid>
    {
        private readonly ILiveSessionRepository _repository;
        private readonly IEventBus _eventBus;

        public CreateTeamHandler(ILiveSessionRepository repository, IEventBus eventBus)
        {
            _repository = repository;
            _eventBus = eventBus;
        }

        public async Task<Guid> Handle(CreateTeamCommand request, CancellationToken cancellationToken)
        {
            var session = await _repository.GetByIdAsync(request.SessionId, cancellationToken);
            if (session == null)
            {
                throw new BusinessException("La sesión de juego especificada no existe.");
            }

            var teamId = Guid.NewGuid();
            var team = Team.Create(teamId, request.TeamName, request.SessionId);

            session.RegisterTeam(team);

            await _repository.AddTeamAsync(team, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            await _eventBus.PublishAsync(
                new TeamRegisteredIntegrationEvent(session.Id, team.Id, team.Name),
                cancellationToken
            );

            return team.Id;
        }
    }
}
