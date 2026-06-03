using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using api.Application.DTOs;

namespace api.Application.Interfaces;

public interface IUserServiceClient
{
    Task<Guid> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserDto>> GetUsersAsync(string? search = null, string? role = null, CancellationToken cancellationToken = default);
    Task UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);
}
