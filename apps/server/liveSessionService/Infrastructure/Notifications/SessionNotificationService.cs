using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using LiveSessionService.Application.Abstractions;
using LiveSessionService.Infrastructure.Notifications.Hubs;

namespace LiveSessionService.Infrastructure.Notifications
{
    public class SessionNotificationService : ISessionNotificationService
    {
        private readonly IHubContext<LiveSessionHub> _hubContext;

        public SessionNotificationService(IHubContext<LiveSessionHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyPlayerJoinedAsync(Guid sessionId, Guid teamId, string displayName, CancellationToken cancellationToken = default)
        {
            await _hubContext.Clients.Group($"session-{sessionId}").SendAsync(
                "PlayerJoined",
                new { SessionId = sessionId, TeamId = teamId, DisplayName = displayName },
                cancellationToken
            );
        }

        public async Task NotifyStateChangedAsync(Guid sessionId, string status, DateTime? endsAt, int? remainingSecondsOnPause, CancellationToken cancellationToken = default)
        {
            await _hubContext.Clients.Group($"session-{sessionId}").SendAsync(
                "StateChanged",
                new { SessionId = sessionId, Status = status, EndsAt = endsAt, RemainingSecondsOnPause = remainingSecondsOnPause },
                cancellationToken
            );
        }

        public async Task NotifyStageAdvancedAsync(Guid sessionId, int currentStageIndex, CancellationToken cancellationToken = default)
        {
            await _hubContext.Clients.Group($"session-{sessionId}").SendAsync(
                "StageAdvanced",
                new { SessionId = sessionId, CurrentStageIndex = currentStageIndex },
                cancellationToken
            );
        }

        public async Task NotifyHintRequestedAsync(Guid sessionId, Guid requestId, Guid teamId, string teamName, Guid subStageId, DateTime requestedAt, CancellationToken cancellationToken = default)
        {
            // Al operador: llega a todo el grupo de la sesión.
            await _hubContext.Clients.Group($"session-{sessionId}").SendAsync(
                "HintRequested",
                new { SessionId = sessionId, RequestId = requestId, TeamId = teamId, TeamName = teamName, SubStageId = subStageId, RequestedAt = requestedAt },
                cancellationToken
            );
        }

        public async Task NotifyHintReleasedAsync(Guid sessionId, Guid teamId, Guid releasedHintId, Guid subStageId, Guid clueId, string text, int order, DateTime releasedAt, CancellationToken cancellationToken = default)
        {
            // SOLO al equipo destinatario: grupo del equipo.
            await _hubContext.Clients.Group($"team-{teamId}").SendAsync(
                "HintReleased",
                new { SessionId = sessionId, TeamId = teamId, ReleasedHintId = releasedHintId, SubStageId = subStageId, ClueId = clueId, Text = text, Order = order, ReleasedAt = releasedAt },
                cancellationToken
            );
        }

        public async Task NotifyHintRequestResolvedAsync(Guid sessionId, Guid teamId, Guid subStageId, CancellationToken cancellationToken = default)
        {
            // Al operador: para retirar la solicitud del panel una vez atendida.
            await _hubContext.Clients.Group($"session-{sessionId}").SendAsync(
                "HintRequestResolved",
                new { SessionId = sessionId, TeamId = teamId, SubStageId = subStageId },
                cancellationToken
            );
        }
    }
}
