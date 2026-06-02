using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UserService.Application.DTOs;

namespace UserService.Application.Interfaces
{
    public interface IIdentityProviderService
    {
        Task<string> CreateUserAsync(string email, string name, string password, string role, CancellationToken cancellationToken = default);
        Task<IEnumerable<UserDto>> GetUsersAsync(string? searchTerm = null, string? role = null, CancellationToken cancellationToken = default);
    }
}
