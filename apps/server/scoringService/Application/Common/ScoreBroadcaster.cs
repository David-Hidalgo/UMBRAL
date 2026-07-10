using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ScoringService.Application.Abstractions;
using ScoringService.Application.IntegrationEvents;
using ScoringService.Domain.Exceptions;
using ScoringService.Domain.Repositories;

namespace ScoringService.Application.Common
{
    /// <summary>
    /// Implementación del recálculo y difusión del ranking. Depende solo de puertos, por lo que
    /// pertenece a la capa de aplicación (orquestación), no a infraestructura.
    /// </summary>
    public sealed class ScoreBroadcaster : IScoreBroadcaster
    {
        private readonly IScoringRepository _scoringRepository;
        private readonly ISessionMonitorRepository _sessionMonitorRepository;
        private readonly IScoringNotificationService _notificationService;
        private readonly IEventBus _eventBus;

        public ScoreBroadcaster(
            IScoringRepository scoringRepository,
            ISessionMonitorRepository sessionMonitorRepository,
            IScoringNotificationService notificationService,
            IEventBus eventBus)
        {
            _scoringRepository = scoringRepository;
            _sessionMonitorRepository = sessionMonitorRepository;
            _notificationService = notificationService;
            _eventBus = eventBus;
        }

        public async Task<int> BroadcastAsync(
            Guid sessionId, Guid teamId, int delta, DateTime occurredAt, CancellationToken cancellationToken = default)
        {
            var monitor = await _sessionMonitorRepository.GetBySessionIdAsync(sessionId, cancellationToken)
                ?? throw new SessionMonitorNotFoundException(sessionId);

            var entries = await _scoringRepository.GetScoreEntriesBySessionAsync(sessionId, cancellationToken);
            var ranking = RankingCalculator.Build(monitor.Teams, entries);
            var teamTotal = ranking.FirstOrDefault(r => r.TeamId == teamId)?.TotalPoints ?? 0;

            await _notificationService.NotifyScoreUpdatedAsync(sessionId, teamId, teamTotal, cancellationToken);
            await _notificationService.NotifyRankingUpdatedAsync(sessionId, ranking, cancellationToken);
            await _eventBus.PublishAsync(
                new ScoreRecalculatedIntegrationEvent(sessionId, teamId, delta, teamTotal, occurredAt),
                cancellationToken);

            return teamTotal;
        }
    }
}
