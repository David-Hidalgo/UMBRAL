using System.Collections.Generic;
using MediatR;
using UserService.Application.DTOs;

namespace UserService.Application.Queries
{
    public sealed record GetUsersQuery(string? SearchTerm, string? Role) : IRequest<IEnumerable<UserDto>>;
}
