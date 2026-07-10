using System;
using System.Collections.Generic;
using System.Linq;
using ScoringService.Domain.Enums;
using ScoringService.Domain.ValueObjects;

namespace ScoringService.Domain.Entities
{
    /// <summary>
    /// Raíz de agregado del lado de "Monitoreo". Es un espejo local del estado de una sesión en
    /// vivo, reconstruido a partir de los eventos de integración de liveSessionService. Permite
    /// hacer cumplir RB-03 (aceptar evidencias solo con sesión activa) y conocer los equipos
    /// participantes para el ranking sin acoplarse en caliente al servicio de sesiones.
    /// </summary>
    public sealed class SessionMonitor
    {
        private readonly List<MonitoredTeam> _teams = new();
        private List<Guid> _activeSubStageIds = new();

        public Guid SessionId { get; private set; }
        public Guid MissionId { get; private set; }
        public string Name { get; private set; } = null!;
        public SessionMonitorStatus Status { get; private set; }

        public IReadOnlyCollection<MonitoredTeam> Teams => _teams.AsReadOnly();
        public IReadOnlyCollection<Guid> ActiveSubStageIds => _activeSubStageIds.AsReadOnly();

        // Requerido por EF Core.
        private SessionMonitor() { }

        public static SessionMonitor Create(Guid sessionId, Guid missionId, string name)
        {
            if (sessionId == Guid.Empty)
                throw new ArgumentException("El identificador de la sesión es requerido.", nameof(sessionId));

            return new SessionMonitor
            {
                SessionId = sessionId,
                MissionId = missionId,
                Name = string.IsNullOrWhiteSpace(name) ? "(sin nombre)" : name.Trim(),
                Status = SessionMonitorStatus.Pending
            };
        }

        /// <summary>Idempotente: registrar el mismo equipo dos veces (reintento de evento) no duplica.</summary>
        public void RegisterTeam(Guid teamId, TeamCode code)
        {
            if (_teams.Any(t => t.TeamId == teamId))
                return;

            _teams.Add(MonitoredTeam.Create(teamId, code));
        }

        public void Activate(IReadOnlyList<Guid> activeSubStageIds)
        {
            Status = SessionMonitorStatus.Active;
            ReplaceActiveSubStages(activeSubStageIds);
        }

        public void Pause() => Status = SessionMonitorStatus.Paused;

        public void Finish()
        {
            Status = SessionMonitorStatus.Finished;
            _activeSubStageIds = new List<Guid>();
        }

        public void Cancel()
        {
            Status = SessionMonitorStatus.Cancelled;
            _activeSubStageIds = new List<Guid>();
        }

        public void TransitionStage(IReadOnlyList<Guid> newSubStageIds)
            => ReplaceActiveSubStages(newSubStageIds);

        /// <summary>RB-03: solo se aceptan evidencias mientras la sesión está activa.</summary>
        public bool AcceptsEvidence() => Status == SessionMonitorStatus.Active;

        public bool HasTeam(Guid teamId) => _teams.Any(t => t.TeamId == teamId);

        public bool IsSubStageActive(Guid subStageId) => _activeSubStageIds.Contains(subStageId);

        private void ReplaceActiveSubStages(IReadOnlyList<Guid> subStageIds)
        {
            _activeSubStageIds = subStageIds is null
                ? new List<Guid>()
                : subStageIds.Distinct().ToList();
        }
    }
}
