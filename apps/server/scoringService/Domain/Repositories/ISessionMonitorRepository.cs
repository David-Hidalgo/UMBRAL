using System;
using System.Threading;
using System.Threading.Tasks;
using ScoringService.Domain.Entities;

namespace ScoringService.Domain.Repositories
{
    /// <summary>
    /// Puerto de persistencia del espejo de monitoreo de sesiones. Lo escribe el consumidor de
    /// eventos de RabbitMQ y lo leen los handlers de puntuación (RB-03) y de ranking.
    /// </summary>
    public interface ISessionMonitorRepository
    {
        Task<SessionMonitor?> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
        Task AddAsync(SessionMonitor monitor, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
