using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using ScoringService.Application.Abstractions;
using ScoringService.Application.DTOs;
using ScoringService.Infrastructure.Notifications.Hubs;

namespace ScoringService.Infrastructure.Notifications
{
    /// <summary>Adaptador de salida de SignalR: emite ranking y puntaje a los grupos de la sesión.</summary>
    public sealed class ScoringNotificationService : IScoringNotificationService
    {
        private readonly IHubContext<ScoringHub> _hubContext;

        public ScoringNotificationService(IHubContext<ScoringHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyRankingUpdatedAsync(Guid sessionId, IReadOnlyList<RankingEntryDto> ranking, CancellationToken cancellationToken = default)
        {
            await _hubContext.Clients.Group(ScoringHub.SessionGroup(sessionId)).SendAsync(
                "RankingUpdated",
                new { SessionId = sessionId, Ranking = ranking },
                cancellationToken);
        }

        public async Task NotifyScoreUpdatedAsync(Guid sessionId, Guid teamId, int totalPoints, CancellationToken cancellationToken = default)
        {
            await _hubContext.Clients.Group(ScoringHub.TeamGroup(sessionId, teamId)).SendAsync(
                "ScoreUpdated",
                new { SessionId = sessionId, TeamId = teamId, TotalPoints = totalPoints },
                cancellationToken);
        }

        public async Task NotifyEvidenceValidatedAsync(Guid sessionId, Guid teamId, Guid submissionId, bool isCorrect, int awardedPoints, CancellationToken cancellationToken = default)
        {
            await _hubContext.Clients.Group(ScoringHub.TeamGroup(sessionId, teamId)).SendAsync(
                "EvidenceValidated",
                new { SessionId = sessionId, TeamId = teamId, SubmissionId = submissionId, IsCorrect = isCorrect, AwardedPoints = awardedPoints },
                cancellationToken);
        }
    }
}
