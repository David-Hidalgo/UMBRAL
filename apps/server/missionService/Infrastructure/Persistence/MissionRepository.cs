using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MissionService.Domain.Entities;
using MissionService.Domain.Enums;
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
                        .ThenInclude(ss => ss.Clues)
                .Include(m => m.Stages)
                    .ThenInclude(s => s.SubStages)
                        .ThenInclude(ss => ((TriviaSubStage)ss).Question)
                            .ThenInclude(q => q.Answers)
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

        public async Task<bool> ExistsByNameExcludingIdAsync(string name, Guid excludeId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var normalized = name.Trim().ToLowerInvariant();
            return await _context.Missions
                .AnyAsync(m => m.Id != excludeId && m.Name.ToLower() == normalized, cancellationToken);
        }

        public async Task AddAsync(Mission mission, CancellationToken cancellationToken = default)
        {
            await _context.Missions.AddAsync(mission, cancellationToken);
        }

        public void Remove(Mission mission)
        {
            _context.Missions.Remove(mission);
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
                if (string.IsNullOrWhiteSpace(status))
                    throw new ArgumentException("Status value cannot be empty.", nameof(status));

                if (Enum.TryParse<MissionState>(status.Trim(), true, out var missionState))
                {
                    query = query.Where(m => m.State == missionState);
                }
                else
                {
                    throw new ArgumentException($"Invalid status value: '{status}'", nameof(status));
                }
            }

            return await query
                .OrderBy(m => m.Name)
                .ToListAsync(cancellationToken);
        }
    }
}
