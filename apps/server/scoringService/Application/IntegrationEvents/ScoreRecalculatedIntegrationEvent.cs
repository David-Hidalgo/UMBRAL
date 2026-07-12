using System;

namespace ScoringService.Application.IntegrationEvents
{
    /// <summary>
    /// Publicado al exchange propio (umbral.scoring) tras recalcular el puntaje de un equipo,
    /// ya sea por validar una evidencia o por aplicar una penalización (RF-10, RF-14).
    /// </summary>
    public sealed record ScoreRecalculatedIntegrationEvent(
        Guid SessionId,
        Guid TeamId,
        int Delta,
        int TotalPoints,
        DateTime OccurredAt);
}
