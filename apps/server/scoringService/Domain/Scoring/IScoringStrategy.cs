using ScoringService.Domain.Enums;
using ScoringService.Domain.ValueObjects;

namespace ScoringService.Domain.Scoring
{
    /// <summary>
    /// Estrategia de cálculo de puntaje (patrón Strategy). Cada nivel de dificultad tiene su
    /// propia implementación, de modo que el puntaje otorgado varía según la dificultad de la
    /// misión (requisito del enunciado). Agregar una nueva dificultad = agregar una estrategia,
    /// sin tocar los handlers (OCP).
    /// </summary>
    public interface IScoringStrategy
    {
        /// <summary>Dificultad que atiende esta estrategia.</summary>
        MissionDifficulty Difficulty { get; }

        /// <summary>Convierte el puntaje base de la etapa en el puntaje efectivo a otorgar.</summary>
        ScoreValue CalculateAward(int basePoints);
    }
}
