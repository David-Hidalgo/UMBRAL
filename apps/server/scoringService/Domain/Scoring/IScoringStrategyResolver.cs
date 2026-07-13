using ScoringService.Domain.Enums;

namespace ScoringService.Domain.Scoring
{
    /// <summary>Selecciona la <see cref="IScoringStrategy"/> correspondiente a una dificultad.</summary>
    public interface IScoringStrategyResolver
    {
        IScoringStrategy Resolve(MissionDifficulty difficulty);
    }
}
