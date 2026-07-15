using System;
using System.Collections.Generic;
using LiveSessionService.Application.DTOs;
using MediatR;

namespace LiveSessionService.Application.Queries
{
    // Para el equipo (móvil): las pistas ya liberadas a ese equipo. Útil al reconectar (RF-06).
    public sealed record GetTeamReleasedHintsQuery(Guid SessionId, Guid TeamId)
        : IRequest<IReadOnlyList<ReleasedHintDto>>;
}
