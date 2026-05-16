namespace UserService.Application.DTOs
{
    public record CreateUserRequest(string Email, string Name, string Password);
}