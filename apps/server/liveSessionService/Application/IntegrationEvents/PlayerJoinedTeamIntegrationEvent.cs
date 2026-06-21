using System;

namespace LiveSessionService.Application.IntegrationEvents
{
    public record PlayerJoinedTeamIntegrationEvent(Guid SessionId, Guid TeamId, string KeycloakUserId, string DisplayName);
}
