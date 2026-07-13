using ScoringService.Domain.Enums;

namespace ScoringService.Domain.Scoring
{
    /// <summary>Dificultad Intermedia: bonifica el puntaje base en un 50% (×1.5).</summary>
    public sealed class IntermediateScoringStrategy : ScoringStrategyBase
    {
        public override MissionDifficulty Difficulty => MissionDifficulty.Intermediate;
        protected override decimal Multiplier => 1.5m;
    }
}
