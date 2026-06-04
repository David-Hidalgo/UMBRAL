using System;
using System.Threading;
using System.Threading.Tasks;
using api.Application.DTOs;

namespace api.Application.Interfaces;

public interface IMissionServiceClient
{
    Task<Guid> CreateMissionAsync(CreateMissionRequest request, CancellationToken cancellationToken = default);
}
