using System;

namespace ScoringService.Domain.Exceptions
{
    /// <summary>
    /// El equipo ya resolvió correctamente este reto: no puede volver a puntuarlo (anti
    /// doble-puntaje). Los intentos previos incorrectos sí permiten reintentar.
    /// </summary>
    public sealed class SubStageAlreadySolvedException : DomainException
    {
        public SubStageAlreadySolvedException(Guid subStageId)
            : base($"Tu equipo ya resolvió correctamente este reto ('{subStageId}').") { }
    }
}
