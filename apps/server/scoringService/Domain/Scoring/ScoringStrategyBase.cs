using System;
using ScoringService.Domain.Enums;
using ScoringService.Domain.ValueObjects;

namespace ScoringService.Domain.Scoring
{
    /// <summary>
    /// Base común de las estrategias de puntaje. Aplica un multiplicador por dificultad sobre el
    /// puntaje base y redondea al entero más cercano (medios hacia arriba). Cada estrategia
    /// concreta solo declara su dificultad y su multiplicador.
    /// </summary>
    public abstract class ScoringStrategyBase : IScoringStrategy
    {
        public abstract MissionDifficulty Difficulty { get; }

        /// <summary>Factor por el que se multiplica el puntaje base de la etapa.</summary>
        protected abstract decimal Multiplier { get; }

        public ScoreValue CalculateAward(int basePoints)
        {
            if (basePoints < 0)
                throw new ArgumentException("El puntaje base no puede ser negativo.", nameof(basePoints));

            var awarded = (int)Math.Round(basePoints * Multiplier, MidpointRounding.AwayFromZero);
            return ScoreValue.Award(awarded);
        }
    }
}
