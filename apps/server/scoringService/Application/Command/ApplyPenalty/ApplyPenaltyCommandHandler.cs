using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ScoringService.Application.Abstractions;
using ScoringService.Application.Commands;
using ScoringService.Domain.Entities;
using ScoringService.Domain.Exceptions;
using ScoringService.Domain.Repositories;
using ScoringService.Domain.ValueObjects;

namespace ScoringService.Application.Handlers
{
    /// <summary>
    /// El operador aplica una penalización justificada a un equipo (RF-11). El motivo es
    /// obligatorio (RB-06, garantizado por el VO PenaltyReason) y queda trazado en el libro
    /// mayor (RB-07). Recalcula y difunde el ranking (RF-10, RF-12).
    /// </summary>
    public sealed class ApplyPenaltyCommandHandler : IRequestHandler<ApplyPenaltyCommand, Unit>
    {
        private readonly IScoringRepository _scoringRepository;
        private readonly ISessionMonitorRepository _sessionMonitorRepository;
        private readonly IScoreBroadcaster _scoreBroadcaster;

        public ApplyPenaltyCommandHandler(
            IScoringRepository scoringRepository,
            ISessionMonitorRepository sessionMonitorRepository,
            IScoreBroadcaster scoreBroadcaster)
        {
            _scoringRepository = scoringRepository;
            _sessionMonitorRepository = sessionMonitorRepository;
            _scoreBroadcaster = scoreBroadcaster;
        }

        public async Task<Unit> Handle(ApplyPenaltyCommand request, CancellationToken cancellationToken)
        {
            var monitor = await _sessionMonitorRepository.GetBySessionIdAsync(request.SessionId, cancellationToken)
                ?? throw new SessionMonitorNotFoundException(request.SessionId);

            if (!monitor.HasTeam(request.TeamId))
                throw new TeamNotInSessionException(request.TeamId, request.SessionId);

            var now = DateTime.UtcNow;
            var reason = new PenaltyReason(request.Reason); // RB-06
            var scoreEntry = ScoreEntry.ForPenalty(
                Guid.NewGuid(), request.SessionId, request.TeamId,
                ScoreValue.Penalty(request.Points), reason, now);

            await _scoringRepository.AddScoreEntryAsync(scoreEntry, cancellationToken);
            await _scoringRepository.AddSessionEventAsync(
                SessionEvent.Record(
                    Guid.NewGuid(), request.SessionId, "PenaltyApplied",
                    $"El operador penalizó al equipo {request.TeamId} con -{request.Points} puntos. Motivo: {reason.Value}",
                    now, request.Actor),
                cancellationToken);

            await _scoringRepository.SaveChangesAsync(cancellationToken);

            await _scoreBroadcaster.BroadcastAsync(
                request.SessionId, request.TeamId, -request.Points, now, cancellationToken);

            return Unit.Value;
        }
    }
}
