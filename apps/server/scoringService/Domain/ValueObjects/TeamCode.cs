using System;

namespace ScoringService.Domain.ValueObjects
{
    /// <summary>
    /// Value Object inmutable que representa la etiqueta identificadora de un equipo dentro de la
    /// sesión (nombre normalizado). La correlación técnica se mantiene por TeamId (Guid).
    /// </summary>
    public record TeamCode
    {
        public string Value { get; }

        public TeamCode(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("El identificador del equipo es requerido.", nameof(value));
            Value = value.Trim();
        }

        public override string ToString() => Value;
    }
}
