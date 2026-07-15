using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LiveSessionService.Domain.Entities;

namespace LiveSessionService.Domain.Repositories
{
    public interface ILiveSessionRepository
    {
        Task<LiveSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Team?> GetTeamByIdAsync(Guid teamId, CancellationToken cancellationToken = default);
        Task AddAsync(LiveSession liveSession, CancellationToken cancellationToken = default);
        Task AddTeamAsync(Team team, CancellationToken cancellationToken = default);
        Task AddPlayerAsync(Player player, CancellationToken cancellationToken = default);
        Task AddHintRequestAsync(HintRequest hintRequest, CancellationToken cancellationToken = default);
        Task AddReleasedHintAsync(ReleasedHint releasedHint, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<LiveSession>> GetAllAsync(Guid? missionId, string? status, CancellationToken cancellationToken = default);
    }
}
