using System;
using System.Collections.Generic;
using LiveSessionService.Application.DTOs;
using MediatR;

namespace LiveSessionService.Application.Queries
{
    // Para el operador: solicitudes de pista de una sesión (opcionalmente filtradas por estado).
    public sealed record GetHintRequestsQuery(Guid SessionId, string? Status = null)
        : IRequest<IReadOnlyList<HintRequestDto>>;
}
