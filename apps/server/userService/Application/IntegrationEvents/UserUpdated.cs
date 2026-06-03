using System;

namespace UserService.Application.IntegrationEvents
{
    public record UserUpdated(Guid UserId, string KeycloakUserId, string Name, string Role);
}
