using System.Collections.Generic;
using System.Linq;
using ScoringService.Domain.Enums;

namespace ScoringService.Domain.Scoring
{
    /// <summary>
    /// Resuelve la estrategia por dificultad a partir del conjunto de estrategias registradas.
    /// Al alimentarse de la colección completa, incorporar una dificultad/estrategia nueva no
    /// requiere modificar este resolver (OCP). Si faltara una, cae en Principiante (×1.0).
    /// </summary>
    public sealed class ScoringStrategyResolver : IScoringStrategyResolver
    {
        private readonly IReadOnlyDictionary<MissionDifficulty, IScoringStrategy> _strategies;
        private readonly IScoringStrategy _fallback;

        public ScoringStrategyResolver(IEnumerable<IScoringStrategy> strategies)
        {
            _strategies = strategies.ToDictionary(s => s.Difficulty);
            _fallback = _strategies.TryGetValue(MissionDifficulty.Beginner, out var beginner)
                ? beginner
                : new BeginnerScoringStrategy();
        }

        public IScoringStrategy Resolve(MissionDifficulty difficulty)
            => _strategies.TryGetValue(difficulty, out var strategy) ? strategy : _fallback;
    }
}
