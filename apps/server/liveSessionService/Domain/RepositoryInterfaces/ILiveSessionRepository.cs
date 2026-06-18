using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LiveSessionService.Domain.Entities;

namespace LiveSessionService.Domain.RepositoryInterfaces
{
    public interface ILiveSessionRepository
    {
        Task<LiveSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(LiveSession liveSession, CancellationToken cancellationToken = default);
        Task AddTeamAsync(Team team, CancellationToken cancellationToken = default);
        Task AddPlayerAsync(Player player, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<LiveSession>> GetAllAsync(Guid? missionId, string? status, CancellationToken cancellationToken = default);
    }
}
