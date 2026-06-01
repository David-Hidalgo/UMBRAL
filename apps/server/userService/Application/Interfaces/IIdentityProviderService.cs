using System.Threading;
using System.Threading.Tasks;

namespace UserService.Application.Interfaces
{
    public interface IIdentityProviderService
    {
        Task<string> CreateUserAsync(string email, string name, string password, string role, CancellationToken cancellationToken = default);
    }
}
