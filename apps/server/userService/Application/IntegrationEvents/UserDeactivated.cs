using System;

namespace UserService.Application.IntegrationEvents;

public record UserDeactivated(Guid UserId, string KeycloakUserId);
