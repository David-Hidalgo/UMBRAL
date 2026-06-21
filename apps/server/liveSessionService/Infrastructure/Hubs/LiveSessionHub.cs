using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LiveSessionService.Infrastructure.Hubs
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
    }
}
