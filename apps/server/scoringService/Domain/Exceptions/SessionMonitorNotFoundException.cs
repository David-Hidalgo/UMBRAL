using System;

namespace ScoringService.Domain.Exceptions
{
    /// <summary>La sesión referida no existe en el espejo de monitoreo local.</summary>
    public sealed class SessionMonitorNotFoundException : DomainException
    {
        public SessionMonitorNotFoundException(Guid sessionId)
            : base($"No se encontró la sesión '{sessionId}' en el monitoreo de puntuación.") { }
    }
}
