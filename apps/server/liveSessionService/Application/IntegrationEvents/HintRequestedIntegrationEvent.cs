using System;

namespace LiveSessionService.Application.IntegrationEvents
{
    public record HintRequestedIntegrationEvent(Guid SessionId, Guid TeamId, Guid SubStageId, DateTime RequestedAt);
}
