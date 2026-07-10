using System;
using System.Collections.Generic;
using ScoringService.Domain.Enums;
using ScoringService.Domain.Events;
using ScoringService.Domain.Exceptions;

namespace ScoringService.Domain.Entities
{
    /// <summary>
    /// Raíz de agregado. Representa la respuesta/evidencia enviada por un equipo para una etapa
    /// concreta de una sesión (RF-08, RF-09). Encapsula el ciclo de validación y sus invariantes.
    /// </summary>
    public sealed class EvidenceSubmission
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        public Guid Id { get; private set; }
        public Guid SessionId { get; private set; }
        public Guid TeamId { get; private set; }
        public Guid SubStageId { get; private set; }
        public SubmissionType Type { get; private set; }
        public string Payload { get; private set; } = null!;
        public SubmissionStatus Status { get; private set; }
        public DateTime SubmittedAt { get; private set; }
        public DateTime? ValidatedAt { get; private set; }
        public int? AwardedPoints { get; private set; }

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
        public void ClearDomainEvents() => _domainEvents.Clear();

        // Requerido por EF Core.
        private EvidenceSubmission() { }

        /// <summary>
        /// RB-05: cada evidencia se asocia exactamente a un equipo, una sesión y una etapa.
        /// La estructura del factory garantiza los tres vínculos obligatorios.
        /// </summary>
        public static EvidenceSubmission Create(
            Guid id,
            Guid sessionId,
            Guid teamId,
            Guid subStageId,
            SubmissionType type,
            string payload,
            DateTime submittedAt)
        {
            if (sessionId == Guid.Empty)
                throw new InvalidSubmissionException("La evidencia debe asociarse a una sesión.");
            if (teamId == Guid.Empty)
                throw new InvalidSubmissionException("La evidencia debe asociarse a un equipo.");
            if (subStageId == Guid.Empty)
                throw new InvalidSubmissionException("La evidencia debe asociarse a una etapa.");
            if (string.IsNullOrWhiteSpace(payload))
                throw new InvalidSubmissionException("La evidencia enviada no puede estar vacía.");

            return new EvidenceSubmission
            {
                Id = id,
                SessionId = sessionId,
                TeamId = teamId,
                SubStageId = subStageId,
                Type = type,
                Payload = payload.Trim(),
                Status = SubmissionStatus.Pending,
                SubmittedAt = submittedAt
            };
        }

        /// <summary>Marca la evidencia como correcta y otorga el puntaje base de la etapa.</summary>
        public void MarkCorrect(int awardedPoints, DateTime validatedAt)
        {
            EnsurePending();
            if (awardedPoints < 0)
                throw new InvalidSubmissionException("El puntaje otorgado no puede ser negativo.");

            Status = SubmissionStatus.Correct;
            AwardedPoints = awardedPoints;
            ValidatedAt = validatedAt;

            _domainEvents.Add(new EvidenceValidatedDomainEvent(
                Id, SessionId, TeamId, SubStageId, awardedPoints, IsCorrect: true, SubmittedAt, validatedAt));
        }

        /// <summary>Marca la evidencia como incorrecta (no otorga puntaje).</summary>
        public void MarkIncorrect(DateTime validatedAt)
        {
            EnsurePending();

            Status = SubmissionStatus.Incorrect;
            AwardedPoints = 0;
            ValidatedAt = validatedAt;

            _domainEvents.Add(new EvidenceValidatedDomainEvent(
                Id, SessionId, TeamId, SubStageId, 0, IsCorrect: false, SubmittedAt, validatedAt));
        }

        private void EnsurePending()
        {
            if (Status != SubmissionStatus.Pending)
                throw new SubmissionAlreadyValidatedException(Id);
        }
    }
}
