using System;

namespace ScoringService.Infrastructure.Persistence.Inbox
{
    /// <summary>
    /// Registro de deduplicación de eventos consumidos (patrón Inbox, backend-spec §4). Garantiza
    /// consumidores idempotentes: un mensaje ya procesado no vuelve a aplicarse ante redeliveries.
    /// </summary>
    public sealed class ProcessedEvent
    {
        public Guid Id { get; private set; }
        public string RoutingKey { get; private set; } = null!;
        public DateTime ProcessedAt { get; private set; }

        private ProcessedEvent() { }

        public static ProcessedEvent Create(Guid id, string routingKey, DateTime processedAt)
            => new() { Id = id, RoutingKey = routingKey, ProcessedAt = processedAt };
    }
}
