using System;

namespace UserService.Application.IntegrationEvents
{
    public record UserCreated(Guid UserId, string KeycloakUserId, string Email);
}
