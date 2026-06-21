using System;
using System.Threading;
using System.Threading.Tasks;
using LiveSessionService.Application.DTOs;

namespace LiveSessionService.Application.Abstractions
{
    public interface IMissionServiceClient
    {
        Task<MissionClientDto?> GetMissionByIdAsync(Guid id, string jwtToken, CancellationToken cancellationToken = default);
    }
}
