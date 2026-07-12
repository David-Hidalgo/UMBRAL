using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ScoringService.Infrastructure.Notifications.Hubs
{
    /// <summary>
    /// Hub de puntuación en tiempo real. Protegido con [Authorize] (skill websocket-signalr §2).
    /// Los clientes se agrupan por sesión, y opcionalmente por equipo, para aislar los flujos
    /// (backend-spec §5).
    /// </summary>
    [Authorize]
    public class ScoringHub : Hub
    {
        public async Task JoinSessionGroup(Guid sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, SessionGroup(sessionId));
        }

        public async Task LeaveSessionGroup(Guid sessionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, SessionGroup(sessionId));
        }

        public async Task JoinTeamGroup(Guid sessionId, Guid teamId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, TeamGroup(sessionId, teamId));
        }

        public async Task LeaveTeamGroup(Guid sessionId, Guid teamId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, TeamGroup(sessionId, teamId));
        }

        public static string SessionGroup(Guid sessionId) => $"Session-{sessionId}";
        public static string TeamGroup(Guid sessionId, Guid teamId) => $"Session-{sessionId}-Team-{teamId}";
    }
}
