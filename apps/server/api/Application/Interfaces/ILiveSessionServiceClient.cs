using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using api.Application.DTOs;

namespace api.Application.Interfaces
{
    public interface ILiveSessionServiceClient
    {
        Task<Guid> CreateSessionAsync(CreateLiveSessionRequest request, CancellationToken cancellationToken = default);
        Task<IEnumerable<LiveSessionSummaryDto>> GetAllSessionsAsync(Guid? missionId, string? status, CancellationToken cancellationToken = default);
        Task<LiveSessionDetailDto> GetSessionByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task TransitionStatusAsync(Guid id, TransitionStatusRequest request, CancellationToken cancellationToken = default);
        Task<Guid> CreateTeamAsync(Guid id, CreateTeamRequest request, CancellationToken cancellationToken = default);
        Task JoinTeamAsync(Guid id, Guid teamId, CancellationToken cancellationToken = default);
        Task AdvanceStageAsync(Guid id, AdvanceStageRequest request, CancellationToken cancellationToken = default);
    }
}
