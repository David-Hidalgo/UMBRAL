using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MissionService.Domain.Entities;

namespace MissionService.Domain.RepositoryInterfaces
{
    public interface IMissionRepository
    {
        Task<Mission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
        Task AddAsync(Mission mission, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a filtered list of missions for the read side (CQRS query).
        /// Filters are optional: passing null for both returns all missions.
        /// </summary>
        Task<IEnumerable<Mission>> GetAllAsync(
            string? searchTerm,
            string? status,
            CancellationToken cancellationToken = default);
    }
}
