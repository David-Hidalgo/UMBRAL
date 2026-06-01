using System;
using System.Threading;
using System.Threading.Tasks;
using api.Application.DTOs;

namespace api.Application.Interfaces;

public interface IUserServiceClient
{
    Task<Guid> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
}
