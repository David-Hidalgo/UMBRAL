using System;
using MediatR;

namespace ScoringService.Application.Commands
{
    /// <summary>
    /// El operador aplica una penalización justificada a un equipo (RF-11). El motivo es
    /// obligatorio (RB-06) y el asiento queda trazado en el libro mayor (RB-07).
    /// </summary>
    public sealed record ApplyPenaltyCommand(
        Guid SessionId,
        Guid TeamId,
        int Points,
        string Reason,
        string? Actor) : IRequest<Unit>;
}
