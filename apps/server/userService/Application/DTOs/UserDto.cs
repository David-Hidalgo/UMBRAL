using System;

namespace UserService.Application.DTOs
{
    public record UserDto(Guid Id, string KeycloakUserId, string Email, string Name);
}
