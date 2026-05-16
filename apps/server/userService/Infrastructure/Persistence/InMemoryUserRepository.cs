using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UserService.Domain.Entities;
using UserService.Domain.RepositoryInterfaces;

namespace UserService.Infrastructure.Persistence
{
    public class InMemoryUserRepository : IUserRepository
    {
        private readonly ConcurrentDictionary<Guid, User> _store = new();

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            _store[user.Id] = user;
            return Task.CompletedTask;
        }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _store.TryGetValue(id, out var user);
            return Task.FromResult(user);
        }
    }
}
