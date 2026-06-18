using System;

namespace LiveSessionService.Application.IntegrationEvents
{
    public record SessionCreatedIntegrationEvent(Guid SessionId, Guid MissionId, string Name);
}
