using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LiveSessionService.Infrastructure.Notifications.Hubs
{
    [Authorize]
    public class LiveSessionHub : Hub
    {
        public async Task JoinSessionGroup(Guid sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"session-{sessionId}");
        }

        public async Task LeaveSessionGroup(Guid sessionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session-{sessionId}");
        }

        // El móvil de un equipo se une a su propio grupo para recibir SOLO sus pistas liberadas.
        public async Task JoinTeamGroup(Guid teamId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"team-{teamId}");
        }

        public async Task LeaveTeamGroup(Guid teamId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"team-{teamId}");
        }
    }
}
