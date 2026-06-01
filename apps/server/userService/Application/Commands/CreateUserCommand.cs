using System;
using MediatR;

namespace UserService.Application.Commands
{
    public record CreateUserCommand(string Email, string Name, string Password, string Role = "player") : IRequest<Guid>;
}
