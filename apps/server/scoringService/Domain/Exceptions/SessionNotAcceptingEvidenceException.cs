using System;

namespace ScoringService.Domain.Exceptions
{
    /// <summary>RB-03: no se aceptan evidencias si la sesión está pausada, finalizada o cancelada.</summary>
    public sealed class SessionNotAcceptingEvidenceException : DomainException
    {
        public SessionNotAcceptingEvidenceException(Guid sessionId)
            : base($"La sesión '{sessionId}' no está activa; no se aceptan evidencias en este momento.") { }
    }
}
