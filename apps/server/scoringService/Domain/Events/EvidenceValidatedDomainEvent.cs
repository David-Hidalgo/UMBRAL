using System;

namespace ScoringService.Domain.Events
{
    /// <summary>
    /// Se registra cuando una evidencia se valida (correcta o incorrecta). El handler lo traduce a
    /// un asiento de puntaje, notificación SignalR y evento de integración (RF-10, RF-14).
    /// </summary>
    public sealed record EvidenceValidatedDomainEvent(
        Guid SubmissionId,
        Guid SessionId,
        Guid TeamId,
        Guid SubStageId,
        int AwardedPoints,
        bool IsCorrect,
        DateTime SubmittedAt,
        DateTime OccurredAt) : IDomainEvent;
}
