using System;

namespace ScoringService.Application.DTOs
{
    /// <summary>Entrada del historial de auditoría de una sesión (RF-15).</summary>
    public sealed record SessionEventDto(
        Guid Id,
        Guid SessionId,
        string Type,
        string Description,
        string? Actor,
        DateTime OccurredAt);
}
