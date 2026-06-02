using System;

namespace api.Application.DTOs;

public sealed record UserDto(Guid Id, string KeycloakUserId, string Email, string Name, string Role, DateTime CreatedAt);