using System;
using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Domain.Entities
{
    // Solicitud de pista hecha por un equipo desde la app móvil (RF-07).
    // El operador la resuelve otorgando una pista concreta.
    public class HintRequest
    {
        public Guid Id { get; private set; }
        public Guid LiveSessionId { get; private set; }
        public Guid TeamId { get; private set; }
        public Guid SubStageId { get; private set; }
        public HintRequestStatus Status { get; private set; }
        public DateTime RequestedAt { get; private set; }
        public DateTime? ResolvedAt { get; private set; }

        // Requerido por EF Core
        private HintRequest() { }

        public static HintRequest Create(Guid id, Guid liveSessionId, Guid teamId, Guid subStageId, DateTime requestedAtUtc)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("El ID de la solicitud de pista no puede estar vacío.", nameof(id));

            if (teamId == Guid.Empty)
                throw new ArgumentException("El ID del equipo es requerido.", nameof(teamId));

            if (subStageId == Guid.Empty)
                throw new ArgumentException("El ID de la subetapa es requerido.", nameof(subStageId));

            return new HintRequest
            {
                Id = id,
                LiveSessionId = liveSessionId,
                TeamId = teamId,
                SubStageId = subStageId,
                Status = HintRequestStatus.Pendiente,
                RequestedAt = requestedAtUtc
            };
        }

        public void Resolve(DateTime resolvedAtUtc)
        {
            if (Status == HintRequestStatus.Resuelta)
                return;

            Status = HintRequestStatus.Resuelta;
            ResolvedAt = resolvedAtUtc;
        }
    }
}
