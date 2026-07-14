using ScoringService.Domain.Enums;

namespace ScoringService.Domain.Scoring
{
    /// <summary>Dificultad Avanzada: duplica el puntaje base (×2.0).</summary>
    public sealed class AdvancedScoringStrategy : ScoringStrategyBase
    {
        public override MissionDifficulty Difficulty => MissionDifficulty.Advanced;
        protected override decimal Multiplier => 2.0m;
    }
}
