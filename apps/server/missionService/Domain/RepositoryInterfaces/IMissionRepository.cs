using System;
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
    }
}
