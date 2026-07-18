using System;
using MediatR;

namespace UserService.Application.Command.CreateUser
{
    public record CreateUserCommand(string Email, string Name, string Role = "operator") : IRequest<Guid>;
}
