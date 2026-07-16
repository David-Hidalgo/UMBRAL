using System.Collections.Generic;

namespace shared.Authentication;

/// <summary>
/// Expone la identidad del invocador de la peticion HTTP en curso.
/// Devuelve nulos y colecciones vacias cuando no hay contexto HTTP
/// (por ejemplo, consumers de RabbitMQ o tareas en segundo plano).
/// </summary>
public interface ICurrentUserService
{
    /// <summary>Identificador del invocador (claim <c>sub</c> de Keycloak).</summary>
    string? UserId { get; }

    /// <summary>Nombre legible del invocador. En cuentas de servicio es <c>service-account-*</c>.</summary>
    string? UserName { get; }

    /// <summary>Roles del invocador. Vacia si es anonimo.</summary>
    IReadOnlyCollection<string> Roles { get; }

    bool IsAuthenticated { get; }
}
