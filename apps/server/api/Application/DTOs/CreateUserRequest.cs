namespace api.Application.DTOs;

public sealed record CreateUserRequest(string Email, string Name, string? Role = null);