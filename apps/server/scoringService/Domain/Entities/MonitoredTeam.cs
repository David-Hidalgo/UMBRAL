using System;
using ScoringService.Domain.ValueObjects;

namespace ScoringService.Domain.Entities
{
    /// <summary>
    /// Equipo observado dentro de una sesión monitoreada. Entidad propiedad (owned) de
    /// <see cref="SessionMonitor"/>; se alimenta del evento TeamRegistered de liveSessionService.
    /// </summary>
    public sealed class MonitoredTeam
    {
        public Guid TeamId { get; private set; }
        public TeamCode Code { get; private set; } = null!;

        // Requerido por EF Core.
        private MonitoredTeam() { }

        public static MonitoredTeam Create(Guid teamId, TeamCode code)
        {
            if (teamId == Guid.Empty)
                throw new ArgumentException("El identificador del equipo es requerido.", nameof(teamId));
            ArgumentNullException.ThrowIfNull(code);

            return new MonitoredTeam { TeamId = teamId, Code = code };
        }
    }
}
