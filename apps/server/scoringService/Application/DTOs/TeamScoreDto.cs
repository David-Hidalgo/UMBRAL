using System;

namespace ScoringService.Application.DTOs
{
    /// <summary>Puntaje actual de un equipo, para la app móvil (RF-06).</summary>
    public sealed record TeamScoreDto(
        Guid SessionId,
        Guid TeamId,
        string TeamCode,
        int TotalPoints);
}
