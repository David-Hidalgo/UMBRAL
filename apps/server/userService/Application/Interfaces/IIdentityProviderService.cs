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

        /// <summary>Devuelve el usuario por su id de Keycloak, o null si no existe (404).</summary>
        Task<UserDto?> GetUserByIdAsync(string keycloakUserId, CancellationToken cancellationToken = default);

        /// <summary>Actualiza el perfil (nombre) del usuario en Keycloak.</summary>
        Task UpdateUserAsync(string keycloakUserId, string name, CancellationToken cancellationToken = default);

        /// <summary>Reemplaza el rol de aplicacion del usuario (quita el previo y asigna el nuevo).</summary>
        Task UpdateUserRoleAsync(string keycloakUserId, string newRole, CancellationToken cancellationToken = default);
    }
}
