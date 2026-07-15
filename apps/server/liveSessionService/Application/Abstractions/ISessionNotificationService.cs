using System;
using System.Threading;
using System.Threading.Tasks;

namespace LiveSessionService.Application.Abstractions
{
    public interface ISessionNotificationService
    {
        Task NotifyPlayerJoinedAsync(Guid sessionId, Guid teamId, string displayName, CancellationToken cancellationToken = default);
        Task NotifyStateChangedAsync(Guid sessionId, string status, DateTime? endsAt, int? remainingSecondsOnPause, CancellationToken cancellationToken = default);
        Task NotifyStageAdvancedAsync(Guid sessionId, int currentStageIndex, CancellationToken cancellationToken = default);

        // Un equipo pidió una pista -> se avisa al operador (grupo de la sesión).
        Task NotifyHintRequestedAsync(Guid sessionId, Guid requestId, Guid teamId, string teamName, Guid subStageId, DateTime requestedAt, CancellationToken cancellationToken = default);

        // El operador liberó una pista -> se envía SOLO al equipo destinatario (grupo del equipo).
        Task NotifyHintReleasedAsync(Guid sessionId, Guid teamId, Guid releasedHintId, Guid subStageId, Guid clueId, string text, int order, DateTime releasedAt, CancellationToken cancellationToken = default);

        // La solicitud del equipo quedó resuelta -> se avisa al operador para retirarla del panel.
        Task NotifyHintRequestResolvedAsync(Guid sessionId, Guid teamId, Guid subStageId, CancellationToken cancellationToken = default);
    }
}
