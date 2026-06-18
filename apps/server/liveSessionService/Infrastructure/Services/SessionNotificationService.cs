using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using LiveSessionService.Application.Interfaces;
using LiveSessionService.Infrastructure.Hubs;

namespace LiveSessionService.Infrastructure.Services
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

        public async Task NotifyStateChangedAsync(Guid sessionId, string status, CancellationToken cancellationToken = default)
        {
            await _hubContext.Clients.Group($"session-{sessionId}").SendAsync(
                "StateChanged",
                new { SessionId = sessionId, Status = status },
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
    }
}
