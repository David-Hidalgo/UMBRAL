using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using LiveSessionService.Application.DTOs;
using LiveSessionService.Application.Queries;
using LiveSessionService.Domain.Repositories;

namespace LiveSessionService.Application.Handlers
{
    public class GetLiveSessionsHandler : IRequestHandler<GetLiveSessionsQuery, IEnumerable<LiveSessionSummaryDto>>
    {
        private readonly ILiveSessionRepository _repository;

        public GetLiveSessionsHandler(ILiveSessionRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<LiveSessionSummaryDto>> Handle(GetLiveSessionsQuery request, CancellationToken cancellationToken)
        {
            var sessions = await _repository.GetAllAsync(request.MissionId, request.Status, cancellationToken);

            return sessions.Select(s => new LiveSessionSummaryDto(
                s.Id,
                s.MissionId,
                s.MissionName,
                s.Name,
                s.ScheduledAt,
                s.Status.ToString(),
                s.Teams.Count,
                s.Teams.Sum(t => t.Players.Count)
            )).ToList();
        }
    }
}
