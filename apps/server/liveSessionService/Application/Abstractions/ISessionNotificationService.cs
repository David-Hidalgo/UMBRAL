using System;
using System.Threading;
using System.Threading.Tasks;

namespace LiveSessionService.Application.Abstractions
{
    public interface ISessionNotificationService
    {
        Task NotifyPlayerJoinedAsync(Guid sessionId, Guid teamId, string displayName, CancellationToken cancellationToken = default);
        Task NotifyStateChangedAsync(Guid sessionId, string status, CancellationToken cancellationToken = default);
        Task NotifyStageAdvancedAsync(Guid sessionId, int currentStageIndex, CancellationToken cancellationToken = default);
    }
}
