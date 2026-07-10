using System;

namespace ScoringService.Domain.Entities
{
    /// <summary>
    /// Registro de auditoría del historial de una sesión (RF-15). Append-only: documenta cada
    /// hecho relevante (activación, evidencia, penalización, transición de etapa, etc.).
    /// </summary>
    public sealed class SessionEvent
    {
        public Guid Id { get; private set; }
        public Guid SessionId { get; private set; }
        public string Type { get; private set; } = null!;
        public string Description { get; private set; } = null!;
        public string? Actor { get; private set; }
        public DateTime OccurredAt { get; private set; }

        // Requerido por EF Core.
        private SessionEvent() { }

        public static SessionEvent Record(
            Guid id, Guid sessionId, string type, string description, DateTime occurredAt, string? actor = null)
        {
            if (sessionId == Guid.Empty)
                throw new ArgumentException("El evento debe asociarse a una sesión.", nameof(sessionId));
            if (string.IsNullOrWhiteSpace(type))
                throw new ArgumentException("El tipo de evento es requerido.", nameof(type));

            return new SessionEvent
            {
                Id = id,
                SessionId = sessionId,
                Type = type,
                Description = description ?? string.Empty,
                Actor = actor,
                OccurredAt = occurredAt
            };
        }
    }
}
