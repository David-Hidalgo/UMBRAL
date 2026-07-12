using System;
using ScoringService.Domain.Enums;
using ScoringService.Domain.ValueObjects;

namespace ScoringService.Domain.Entities
{
    /// <summary>
    /// Asiento inmutable del libro mayor de puntaje (append-only). Cada cambio de puntos de un
    /// equipo queda registrado con su origen, garantizando la trazabilidad exigida por RB-07.
    /// El puntaje total de un equipo es la suma de sus asientos.
    /// </summary>
    public sealed class ScoreEntry
    {
        public Guid Id { get; private set; }
        public Guid SessionId { get; private set; }
        public Guid TeamId { get; private set; }
        public ScoreValue Value { get; private set; } = null!;
        public ScoreOrigin Origin { get; private set; }
        public Guid? SourceSubmissionId { get; private set; }
        public PenaltyReason? Reason { get; private set; }
        public DateTime OccurredAt { get; private set; }

        // Requerido por EF Core.
        private ScoreEntry() { }

        /// <summary>Asiento originado por la validación de una evidencia (RB-07: trazable al envío).</summary>
        public static ScoreEntry ForEvidence(
            Guid id, Guid sessionId, Guid teamId, ScoreValue value, Guid submissionId, DateTime occurredAt)
        {
            if (submissionId == Guid.Empty)
                throw new ArgumentException("El origen de la evidencia es obligatorio para la trazabilidad.", nameof(submissionId));

            return new ScoreEntry
            {
                Id = id,
                SessionId = sessionId,
                TeamId = teamId,
                Value = value,
                Origin = ScoreOrigin.EvidenceValidated,
                SourceSubmissionId = submissionId,
                OccurredAt = occurredAt
            };
        }

        /// <summary>Asiento originado por una penalización del operador (RB-06: motivo obligatorio).</summary>
        public static ScoreEntry ForPenalty(
            Guid id, Guid sessionId, Guid teamId, ScoreValue value, PenaltyReason reason, DateTime occurredAt)
        {
            ArgumentNullException.ThrowIfNull(reason);

            return new ScoreEntry
            {
                Id = id,
                SessionId = sessionId,
                TeamId = teamId,
                Value = value,
                Origin = ScoreOrigin.Penalty,
                Reason = reason,
                OccurredAt = occurredAt
            };
        }
    }
}
