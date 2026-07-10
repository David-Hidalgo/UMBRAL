using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ScoringService.Application.DTOs;

namespace ScoringService.Application.Abstractions
{
    /// <summary>
    /// Puerto de notificaciones en tiempo real (SignalR). Sincroniza el ranking y el puntaje con
    /// los clientes conectados sin polling (RF-12, RF-13).
    /// </summary>
    public interface IScoringNotificationService
    {
        Task NotifyRankingUpdatedAsync(Guid sessionId, IReadOnlyList<RankingEntryDto> ranking, CancellationToken cancellationToken = default);
        Task NotifyScoreUpdatedAsync(Guid sessionId, Guid teamId, int totalPoints, CancellationToken cancellationToken = default);
        Task NotifyEvidenceValidatedAsync(Guid sessionId, Guid teamId, Guid submissionId, bool isCorrect, int awardedPoints, CancellationToken cancellationToken = default);
    }
}
