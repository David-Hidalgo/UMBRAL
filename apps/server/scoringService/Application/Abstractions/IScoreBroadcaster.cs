using System;
using System.Threading;
using System.Threading.Tasks;

namespace ScoringService.Application.Abstractions
{
    /// <summary>
    /// Servicio de aplicación que recalcula el ranking de una sesión, lo emite por SignalR
    /// (RF-12) y publica el evento de integración de recálculo (RF-14). Centraliza esa lógica
    /// para reutilizarla desde los handlers de validación y penalización. Devuelve el nuevo
    /// puntaje total del equipo afectado.
    /// </summary>
    public interface IScoreBroadcaster
    {
        Task<int> BroadcastAsync(Guid sessionId, Guid teamId, int delta, DateTime occurredAt, CancellationToken cancellationToken = default);
    }
}
