using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MissionService.Domain.Entities;
using MissionService.Domain.RepositoryInterfaces;

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
                    .ThenInclude(s => s.SubStages.OfType<TriviaSubStage>())
                        .ThenInclude(t => t.Question)
                            .ThenInclude(q => q.Answers)
                .Include(m => m.Stages)
                    .ThenInclude(s => s.SubStages.OfType<TreasureHuntSubStage>())
                        .ThenInclude(th => th.Clues)
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
    }
}
