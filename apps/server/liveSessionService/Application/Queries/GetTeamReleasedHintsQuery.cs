using System;
using System.Collections.Generic;
using LiveSessionService.Application.DTOs;
using MediatR;

namespace LiveSessionService.Application.Queries
{
    // Para el equipo (móvil): las pistas ya liberadas a ese equipo. Útil al reconectar (RF-06).
    // RequestingUserId + IsPrivileged permiten al handler autorizar el acceso: solo un
    // admin/operator (IsPrivileged) o un miembro del propio equipo pueden leer estas pistas.
    public sealed record GetTeamReleasedHintsQuery(
        Guid SessionId,
        Guid TeamId,
        string RequestingUserId,
        bool IsPrivileged)
        : IRequest<IReadOnlyList<ReleasedHintDto>>;
}
