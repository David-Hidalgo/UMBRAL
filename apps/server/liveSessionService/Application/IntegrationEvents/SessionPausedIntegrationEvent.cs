using System;

namespace LiveSessionService.Application.IntegrationEvents
{
    public record SessionPausedIntegrationEvent(Guid SessionId);
}
