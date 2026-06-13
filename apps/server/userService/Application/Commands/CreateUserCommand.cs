using System;
using MediatR;

namespace UserService.Application.Commands
{
    public record CreateUserCommand(string Email, string Name, string Role = "operator") : IRequest<Guid>;
}
