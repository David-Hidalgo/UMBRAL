using System;

namespace ScoringService.Domain.Exceptions
{
    /// <summary>
    /// Excepción base para todas las violaciones de reglas de negocio del dominio de puntuación.
    /// El middleware global la mapea a códigos HTTP semánticos (backend-spec §3).
    /// </summary>
    public abstract class DomainException : Exception
    {
        protected DomainException(string message) : base(message) { }
    }
}
