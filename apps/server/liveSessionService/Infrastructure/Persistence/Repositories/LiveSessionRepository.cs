using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Repositories;

namespace LiveSessionService.Infrastructure.Persistence
{
    public class LiveSessionRepository : ILiveSessionRepository
    {
        private readonly LiveSessionDbContext _context;

        public LiveSessionRepository(LiveSessionDbContext context)
        {
            _context = context;
        }

        public async Task<LiveSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.LiveSessions
                .Include(s => s.Teams)
                    .ThenInclude(t => t.Players)
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        }

        public async Task AddAsync(LiveSession liveSession, CancellationToken cancellationToken = default)
        {
            await _context.LiveSessions.AddAsync(liveSession, cancellationToken);
        }

        public async Task AddTeamAsync(Team team, CancellationToken cancellationToken = default)
        {
            await _context.Teams.AddAsync(team, cancellationToken);
        }

        public async Task AddPlayerAsync(Player player, CancellationToken cancellationToken = default)
        {
            await _context.Players.AddAsync(player, cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IEnumerable<LiveSession>> GetAllAsync(Guid? missionId, string? status, CancellationToken cancellationToken = default)
        {
            var query = _context.LiveSessions
                .Include(s => s.Teams)
                    .ThenInclude(t => t.Players)
                .AsQueryable();

            if (missionId.HasValue)
            {
                query = query.Where(s => s.MissionId == missionId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (Enum.TryParse<LiveSessionStatus>(status, true, out var statusEnum))
                {
                    query = query.Where(s => s.Status == statusEnum);
                }
            }

            return await query.ToListAsync(cancellationToken);
        }
    }
}
