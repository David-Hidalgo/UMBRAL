using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using LiveSessionService.Application.DTOs;
using LiveSessionService.Application.Queries;
using LiveSessionService.Domain.Repositories;

namespace LiveSessionService.Application.Handlers
{
    public class GetLiveSessionByIdHandler : IRequestHandler<GetLiveSessionByIdQuery, LiveSessionDetailDto?>
    {
        private readonly ILiveSessionRepository _repository;

        public GetLiveSessionByIdHandler(ILiveSessionRepository repository)
        {
            _repository = repository;
        }

        public async Task<LiveSessionDetailDto?> Handle(GetLiveSessionByIdQuery request, CancellationToken cancellationToken)
        {
            var session = await _repository.GetByIdAsync(request.Id, cancellationToken);
            if (session == null)
            {
                return null;
            }

            return new LiveSessionDetailDto(
                session.Id,
                session.MissionId,
                session.MissionName,
                session.Name,
                session.ScheduledAt,
                session.CurrentStageIndex,
                session.Status.ToString(),
                session.Teams.Select(t => new TeamDto(
                    t.Id,
                    t.Name,
                    t.Players.Select(p => new PlayerDto(
                        p.Id,
                        p.KeycloakUserId,
                        p.DisplayName,
                        p.JoinedAt
                    )).ToList()
                )).ToList()
            );
        }
    }
}
