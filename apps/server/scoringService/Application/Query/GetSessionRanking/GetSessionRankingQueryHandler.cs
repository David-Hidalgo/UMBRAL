using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ScoringService.Application.Common;
using ScoringService.Application.DTOs;
using ScoringService.Application.Queries;
using ScoringService.Domain.Exceptions;
using ScoringService.Domain.Repositories;

namespace ScoringService.Application.Handlers
{
    public sealed class GetSessionRankingQueryHandler
        : IRequestHandler<GetSessionRankingQuery, IReadOnlyList<RankingEntryDto>>
    {
        private readonly IScoringRepository _scoringRepository;
        private readonly ISessionMonitorRepository _sessionMonitorRepository;

        public GetSessionRankingQueryHandler(
            IScoringRepository scoringRepository,
            ISessionMonitorRepository sessionMonitorRepository)
        {
            _scoringRepository = scoringRepository;
            _sessionMonitorRepository = sessionMonitorRepository;
        }

        public async Task<IReadOnlyList<RankingEntryDto>> Handle(GetSessionRankingQuery request, CancellationToken cancellationToken)
        {
            var monitor = await _sessionMonitorRepository.GetBySessionIdAsync(request.SessionId, cancellationToken)
                ?? throw new SessionMonitorNotFoundException(request.SessionId);

            var entries = await _scoringRepository.GetScoreEntriesBySessionAsync(request.SessionId, cancellationToken);
            return RankingCalculator.Build(monitor.Teams, entries);
        }
    }
}
