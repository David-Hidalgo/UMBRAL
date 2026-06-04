using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MissionService.Domain.Entities;
using MissionService.Domain.RepositoryInterfaces;
using MissionService.Domain.ValueObjects;

namespace MissionService.Infrastructure.Persistence
{
    public class MissionRepository : IMissionRepository
    {
        private readonly MissionDbContext _context;

        public MissionRepository(MissionDbContext context)
        {
            _context = context;
        }

        public async Task<Mission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Missions
                .Include(m => m.Stages)
                    .ThenInclude(s => s.SubStages)
                .Include(m => m.Stages)
                    .ThenInclude(s => s.SubStages)
                        .ThenInclude(ss => ((TriviaSubStage)ss).Question)
                            .ThenInclude(q => q.Answers)
                .Include(m => m.Stages)
                    .ThenInclude(s => s.SubStages)
                        .ThenInclude(ss => ((TreasureHuntSubStage)ss).Clues)
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        }

        public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var normalized = name.Trim().ToLowerInvariant();
            return await _context.Missions
                .AnyAsync(m => m.Name.ToLower() == normalized, cancellationToken);
        }

        public async Task AddAsync(Mission mission, CancellationToken cancellationToken = default)
        {
            await _context.Missions.AddAsync(mission, cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IEnumerable<Mission>> GetAllAsync(
            string? searchTerm,
            string? status,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Missions
                .AsNoTracking()
                .Include(m => m.Stages)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLowerInvariant();
                query = query.Where(m => m.Name.ToLower().Contains(term));
            }

            if (status is not null)
            {
                // Convert to MissionStatus so EF Core can apply the registered value
                // converter (s => s.Value) when building the parameterized SQL WHERE clause.
                var missionStatus = MissionStatus.FromString(status.Trim());
                query = query.Where(m => m.Status == missionStatus);
            }

            return await query
                .OrderBy(m => m.Name)
                .ToListAsync(cancellationToken);
        }
    }
}
