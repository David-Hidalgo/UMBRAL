using System;

namespace ScoringService.Domain.Events
{
    /// <summary>Marcador para eventos de dominio internos (skill ddd-modeling §4).</summary>
    public interface IDomainEvent
    {
        DateTime OccurredAt { get; }
    }
}
