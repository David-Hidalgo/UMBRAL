using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using api.Application.DTOs;

namespace api.Application.Interfaces;

public interface IMissionServiceClient
{
    Task<Guid> CreateMissionAsync(CreateMissionRequest request, CancellationToken cancellationToken = default);

    Task<IEnumerable<MissionSummaryDto>> GetMissionsAsync(
        string? searchTerm,
        string? status,
        CancellationToken cancellationToken = default);

    Task<MissionDetailDto> GetMissionByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task UpdateMissionAsync(Guid id, UpdateMissionRequest request, CancellationToken cancellationToken = default);

    Task DeleteMissionAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Guid> AddStageAsync(Guid id, CreateStageRequest request, CancellationToken cancellationToken = default);

    Task UpdateStageAsync(Guid id, Guid stageId, CreateStageRequest request, CancellationToken cancellationToken = default);

    Task DeleteStageAsync(Guid id, Guid stageId, CancellationToken cancellationToken = default);

    Task ReorderStagesAsync(Guid id, List<Guid> stageIds, CancellationToken cancellationToken = default);
}
