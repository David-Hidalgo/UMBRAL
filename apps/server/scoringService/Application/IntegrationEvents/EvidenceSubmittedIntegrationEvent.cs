using System;

namespace ScoringService.Application.IntegrationEvents
{
    /// <summary>
    /// Publicado al exchange propio (umbral.scoring) cuando se registra una evidencia (RF-14).
    /// Permite que otros procesos (auditoría, tableros) reaccionen de forma desacoplada.
    /// </summary>
    public sealed record EvidenceSubmittedIntegrationEvent(
        Guid SubmissionId,
        Guid SessionId,
        Guid TeamId,
        Guid SubStageId,
        string Status,
        DateTime SubmittedAt);
}
