using System;
using LiveSessionService.Domain.Entities;

namespace LiveSessionService.Tests.Support
{
    /// <summary>
    /// Utilidades para construir sesiones "listas para activar" en las pruebas. Registra un equipo
    /// junto con al menos un jugador, cumpliendo la invariante de <c>PreparationState.Activate</c>
    /// (una sesión no puede iniciar con equipos vacíos). Evita duplicar el andamiaje en cada test.
    /// </summary>
    internal static class LiveSessionTestExtensions
    {
        /// <summary>
        /// Registra un equipo con un jugador en la sesión y devuelve el equipo creado.
        /// </summary>
        public static Team RegisterTeamWithPlayer(
            this LiveSession session,
            string teamName = "Equipo",
            string displayName = "Jugador")
        {
            var team = Team.Create(Guid.NewGuid(), teamName, session.Id);
            session.RegisterTeam(team);
            session.AddPlayerToTeam(
                team.Id,
                Player.Create(Guid.NewGuid(), $"user-{team.Id}", displayName, team.Id));
            return team;
        }
    }
}
