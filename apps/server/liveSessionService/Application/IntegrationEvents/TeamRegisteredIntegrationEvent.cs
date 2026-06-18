using System;

namespace LiveSessionService.Application.IntegrationEvents
{
    public record TeamRegisteredIntegrationEvent(Guid SessionId, Guid TeamId, string TeamName);
}
