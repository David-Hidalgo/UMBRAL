namespace api.Application.DTOs;

public sealed record CreateUserRequest(string Email, string Name, string Password, string? Role = null);