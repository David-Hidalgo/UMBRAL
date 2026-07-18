using System.Collections.Generic;
using MediatR;
using UserService.Application.DTOs;

namespace UserService.Application.Query.GetUsers
{
    public sealed record GetUsersQuery(string? SearchTerm, string? Role) : IRequest<IEnumerable<UserDto>>;
}
