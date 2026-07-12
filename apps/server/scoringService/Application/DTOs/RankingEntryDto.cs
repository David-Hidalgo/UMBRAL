using System;

namespace ScoringService.Application.DTOs
{
    /// <summary>Fila del ranking de una sesión (RF-12). Ordenado según RB-08.</summary>
    public sealed record RankingEntryDto(
        int Rank,
        Guid TeamId,
        string TeamCode,
        int TotalPoints,
        DateTime? LastScoredAt);
}
