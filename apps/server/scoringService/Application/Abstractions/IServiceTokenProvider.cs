using System.Threading;
using System.Threading.Tasks;

namespace ScoringService.Application.Abstractions
{
    /// <summary>
    /// Provee un token de acceso de la propia identidad de servicio del scoring (client
    /// credentials en Keycloak). Se usa para leer el contenido de misiones sin depender del
    /// token del jugador, evitando exponerle las respuestas.
    /// </summary>
    public interface IServiceTokenProvider
    {
        Task<string> GetTokenAsync(CancellationToken cancellationToken = default);
    }
}
