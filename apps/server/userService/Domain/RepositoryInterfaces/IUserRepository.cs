using System;
using System.Threading;
using System.Threading.Tasks;
using UserService.Domain.Entities;

namespace UserService.Domain.RepositoryInterfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(User user, CancellationToken cancellationToken = default);
        Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    }
}
