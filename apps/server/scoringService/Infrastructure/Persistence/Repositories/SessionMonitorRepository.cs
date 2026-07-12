using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ScoringService.Domain.Entities;
using ScoringService.Domain.Repositories;
using ScoringService.Infrastructure.Persistence.Contexts;

namespace ScoringService.Infrastructure.Persistence.Repositories
{
    public sealed class SessionMonitorRepository : ISessionMonitorRepository
    {
        private readonly ScoringDbContext _context;

        public SessionMonitorRepository(ScoringDbContext context)
        {
            _context = context;
        }

        // Los equipos (owned) se incluyen automáticamente por EF. Se rastrea para permitir mutaciones.
        public async Task<SessionMonitor?> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
            => await _context.SessionMonitors.FirstOrDefaultAsync(m => m.SessionId == sessionId, cancellationToken);

        public async Task AddAsync(SessionMonitor monitor, CancellationToken cancellationToken = default)
            => await _context.SessionMonitors.AddAsync(monitor, cancellationToken);

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => await _context.SaveChangesAsync(cancellationToken);
    }
}
