using System;
using System.Collections.Generic;
using MediatR;
using ScoringService.Application.DTOs;

namespace ScoringService.Application.Queries
{
    /// <summary>Historial de auditoría de una sesión (RF-15).</summary>
    public sealed record GetSessionEventsQuery(Guid SessionId) : IRequest<IReadOnlyList<SessionEventDto>>;
}
