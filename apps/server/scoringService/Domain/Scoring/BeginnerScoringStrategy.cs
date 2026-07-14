using ScoringService.Domain.Enums;

namespace ScoringService.Domain.Scoring
{
    /// <summary>Dificultad Principiante: otorga el puntaje base sin bonificación (×1.0).</summary>
    public sealed class BeginnerScoringStrategy : ScoringStrategyBase
    {
        public override MissionDifficulty Difficulty => MissionDifficulty.Beginner;
        protected override decimal Multiplier => 1.0m;
    }
}
