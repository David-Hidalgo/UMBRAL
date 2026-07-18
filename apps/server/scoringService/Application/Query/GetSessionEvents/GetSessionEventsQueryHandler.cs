using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ScoringService.Application.DTOs;
using ScoringService.Application.Queries;
using ScoringService.Domain.Repositories;

namespace ScoringService.Application.Handlers
{
    public sealed class GetSessionEventsQueryHandler
        : IRequestHandler<GetSessionEventsQuery, IReadOnlyList<SessionEventDto>>
    {
        private readonly IScoringRepository _scoringRepository;

        public GetSessionEventsQueryHandler(IScoringRepository scoringRepository)
        {
            _scoringRepository = scoringRepository;
        }

        public async Task<IReadOnlyList<SessionEventDto>> Handle(GetSessionEventsQuery request, CancellationToken cancellationToken)
        {
            var events = await _scoringRepository.GetSessionEventsAsync(request.SessionId, cancellationToken);
            return events
                .Select(e => new SessionEventDto(e.Id, e.SessionId, e.Type, e.Description, e.Actor, e.OccurredAt))
                .ToList();
        }
    }
}
