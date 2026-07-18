using System;
using MediatR;

namespace UserService.Application.Command.UpdateUser
{
    /// <summary>
    /// HU52 - Actualiza el perfil (Nombre y Rol) de un usuario existente.
    /// <paramref name="Id"/> es el identificador de Keycloak (fuente de verdad).
    /// Como Command CQRS no devuelve entidades, solo confirma con <see cref="Unit"/>.
    /// </summary>
    public record UpdateUserCommand(Guid Id, string Name, string Role) : IRequest<Unit>;
}
