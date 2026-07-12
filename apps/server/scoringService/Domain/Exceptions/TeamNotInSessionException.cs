using System;

namespace ScoringService.Domain.Exceptions
{
    /// <summary>El equipo no pertenece a la sesión indicada (coherencia RB-05).</summary>
    public sealed class TeamNotInSessionException : DomainException
    {
        public TeamNotInSessionException(Guid teamId, Guid sessionId)
            : base($"El equipo '{teamId}' no está registrado en la sesión '{sessionId}'.") { }
    }
}
