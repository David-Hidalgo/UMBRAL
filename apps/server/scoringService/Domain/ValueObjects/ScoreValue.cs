using System;

namespace ScoringService.Domain.ValueObjects
{
    /// <summary>
    /// Value Object inmutable que representa una cantidad de puntos (delta). Puede ser
    /// negativa cuando corresponde a una penalización. Se mapea con ComplexProperty (backend-spec §2).
    /// </summary>
    public record ScoreValue
    {
        public int Points { get; }

        // Constructor abierto: EF lo usa al materializar y Add() puede producir totales/negativos.
        public ScoreValue(int points)
        {
            Points = points;
        }

        public static ScoreValue Zero => new(0);

        /// <summary>Puntos otorgados por una evidencia correcta (no negativos).</summary>
        public static ScoreValue Award(int points)
        {
            if (points < 0)
                throw new ArgumentException("El puntaje otorgado no puede ser negativo.", nameof(points));
            return new ScoreValue(points);
        }

        /// <summary>Convierte una cantidad positiva en un delta negativo de penalización.</summary>
        public static ScoreValue Penalty(int points)
        {
            if (points <= 0)
                throw new ArgumentException("La penalización debe restar una cantidad positiva de puntos.", nameof(points));
            return new ScoreValue(-points);
        }

        public ScoreValue Add(ScoreValue other) => new(Points + other.Points);

        public override string ToString() => Points.ToString();
    }
}
