using System;

namespace ScoringService.Domain.Events
{
    /// <summary>Se registra cuando el operador aplica una penalización a un equipo (RF-11).</summary>
    public sealed record PenaltyAppliedDomainEvent(
        Guid SessionId,
        Guid TeamId,
        int Points,
        string Reason,
        DateTime OccurredAt) : IDomainEvent;
}
