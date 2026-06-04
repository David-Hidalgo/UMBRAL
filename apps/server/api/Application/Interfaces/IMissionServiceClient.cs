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
}
