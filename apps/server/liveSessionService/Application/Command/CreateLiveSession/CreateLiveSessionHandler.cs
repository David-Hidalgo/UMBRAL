using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using LiveSessionService.Application.Commands;
using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.IntegrationEvents;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.Repositories;

namespace LiveSessionService.Application.Handlers
{
    public class CreateLiveSessionHandler : IRequestHandler<CreateLiveSessionCommand, Guid>
    {
        private readonly ILiveSessionRepository _repository;
        private readonly IMissionServiceClient _missionClient;
        private readonly IEventBus _eventBus;

        public CreateLiveSessionHandler(
            ILiveSessionRepository repository,
            IMissionServiceClient missionClient,
            IEventBus eventBus)
        {
            _repository = repository;
            _missionClient = missionClient;
            _eventBus = eventBus;
        }

        public async Task<Guid> Handle(CreateLiveSessionCommand request, CancellationToken cancellationToken)
        {
            // 1. Consultar el microservicio de misiones
            var mission = await _missionClient.GetMissionByIdAsync(request.MissionId, request.JwtToken, cancellationToken);
            if (mission == null)
            {
                throw new BusinessException("La misión especificada no existe.");
            }

            // RB-01: Una misión solo puede utilizarse para crear sesiones si se encuentra activa.
            if (!string.Equals(mission.State, "Active", StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessException("La misión debe estar activa para poder crear una sesión de juego.");
            }

            // 2. Crear la sesión
            var sessionId = Guid.NewGuid();
            var liveSession = LiveSession.Create(
                sessionId,
                mission.Id,
                mission.Name,
                request.Name,
                request.ScheduledAt,
                mission.MaxTimeMinutes
            );

            await _repository.AddAsync(liveSession, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            // 3. Publicar evento de integración
            await _eventBus.PublishAsync(
                new SessionCreatedIntegrationEvent(liveSession.Id, liveSession.MissionId, liveSession.Name),
                cancellationToken
            );

            return liveSession.Id;
        }
    }
}
