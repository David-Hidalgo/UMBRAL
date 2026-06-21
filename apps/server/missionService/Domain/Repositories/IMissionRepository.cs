using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MissionService.Domain.Entities;

namespace MissionService.Domain.Repositories
{
    public interface IMissionRepository
    {
        Task<Mission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
        Task<bool> ExistsByNameExcludingIdAsync(string name, Guid excludeId, CancellationToken cancellationToken = default);
        Task AddAsync(Mission mission, CancellationToken cancellationToken = default);
        void Remove(Mission mission);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Mission>> GetAllAsync(string? searchTerm, string? status, CancellationToken cancellationToken = default);
    }
}
