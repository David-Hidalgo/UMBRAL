using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ScoringService.Application.DTOs;
using ScoringService.Application.Queries;
using ScoringService.Domain.Exceptions;
using ScoringService.Domain.Repositories;

namespace ScoringService.Application.Handlers
{
    public sealed class GetTeamScoreQueryHandler : IRequestHandler<GetTeamScoreQuery, TeamScoreDto>
    {
        private readonly IScoringRepository _scoringRepository;
        private readonly ISessionMonitorRepository _sessionMonitorRepository;

        public GetTeamScoreQueryHandler(
            IScoringRepository scoringRepository,
            ISessionMonitorRepository sessionMonitorRepository)
        {
            _scoringRepository = scoringRepository;
            _sessionMonitorRepository = sessionMonitorRepository;
        }

        public async Task<TeamScoreDto> Handle(GetTeamScoreQuery request, CancellationToken cancellationToken)
        {
            var monitor = await _sessionMonitorRepository.GetBySessionIdAsync(request.SessionId, cancellationToken)
                ?? throw new SessionMonitorNotFoundException(request.SessionId);

            var team = monitor.Teams.FirstOrDefault(t => t.TeamId == request.TeamId)
                ?? throw new TeamNotInSessionException(request.TeamId, request.SessionId);

            var entries = await _scoringRepository.GetScoreEntriesByTeamAsync(
                request.SessionId, request.TeamId, cancellationToken);
            var total = entries.Sum(e => e.Value.Points);

            return new TeamScoreDto(request.SessionId, request.TeamId, team.Code.Value, total);
        }
    }
}
