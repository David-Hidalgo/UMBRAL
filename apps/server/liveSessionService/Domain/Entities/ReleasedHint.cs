using System;

namespace LiveSessionService.Domain.Entities
{
    // Pista efectivamente liberada por el operador a un equipo concreto (RF-06/RF-07).
    // Se guarda un snapshot del texto y el orden para no depender del missionService
    // al reconectar o auditar. La unicidad (sesión, equipo, pista) garantiza RB-04.
    public class ReleasedHint
    {
        public Guid Id { get; private set; }
        public Guid LiveSessionId { get; private set; }
        public Guid TeamId { get; private set; }
        public Guid SubStageId { get; private set; }
        public Guid ClueId { get; private set; }
        public string ClueText { get; private set; } = null!;
        public int ClueOrder { get; private set; }
        public DateTime ReleasedAt { get; private set; }

        // Requerido por EF Core
        private ReleasedHint() { }

        public static ReleasedHint Create(
            Guid id,
            Guid liveSessionId,
            Guid teamId,
            Guid subStageId,
            Guid clueId,
            string clueText,
            int clueOrder,
            DateTime releasedAtUtc)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("El ID de la pista liberada no puede estar vacío.", nameof(id));

            if (teamId == Guid.Empty)
                throw new ArgumentException("El ID del equipo es requerido.", nameof(teamId));

            if (subStageId == Guid.Empty)
                throw new ArgumentException("El ID de la subetapa es requerido.", nameof(subStageId));

            if (clueId == Guid.Empty)
                throw new ArgumentException("El ID de la pista es requerido.", nameof(clueId));

            if (string.IsNullOrWhiteSpace(clueText))
                throw new ArgumentException("El texto de la pista es requerido.", nameof(clueText));

            return new ReleasedHint
            {
                Id = id,
                LiveSessionId = liveSessionId,
                TeamId = teamId,
                SubStageId = subStageId,
                ClueId = clueId,
                ClueText = clueText,
                ClueOrder = clueOrder,
                ReleasedAt = releasedAtUtc
            };
        }
    }
}
