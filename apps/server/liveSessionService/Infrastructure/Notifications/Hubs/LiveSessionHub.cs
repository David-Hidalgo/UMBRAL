using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using LiveSessionService.Domain.Repositories;

namespace LiveSessionService.Infrastructure.Notifications.Hubs
{
    [Authorize]
    public class LiveSessionHub : Hub
    {
        private readonly ILiveSessionRepository _repository;

        public LiveSessionHub(ILiveSessionRepository repository)
        {
            _repository = repository;
        }

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
            var keycloakUserId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                 ?? Context.User?.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(keycloakUserId))
            {
                throw new HubException("No se pudo identificar al usuario desde el token JWT.");
            }

            var team = await _repository.GetTeamByIdAsync(teamId);
            if (team == null)
            {
                throw new HubException("El equipo especificado no pertenece a ninguna sesión o no existe.");
            }

            var isPlayerInTeam = team.Players.Any(p => p.KeycloakUserId == keycloakUserId);
            if (!isPlayerInTeam)
            {
                throw new HubException("No tienes permiso para unirte a este equipo.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"team-{teamId}");
        }

        public async Task LeaveTeamGroup(Guid teamId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"team-{teamId}");
        }
    }
}
