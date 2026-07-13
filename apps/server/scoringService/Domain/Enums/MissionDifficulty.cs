using System;

namespace ScoringService.Domain.Enums
{
    /// <summary>
    /// Nivel de dificultad de la misión, réplica propia del bounded context (no se comparte el
    /// enum de missionService para preservar la autonomía del contexto). Determina qué estrategia
    /// de puntaje se aplica al otorgar puntos por una evidencia correcta.
    /// </summary>
    public enum MissionDifficulty
    {
        Beginner = 0,
        Intermediate = 1,
        Advanced = 2
    }

    public static class MissionDifficultyExtensions
    {
        /// <summary>
        /// Traduce el texto de dificultad que envía missionService. Ante un valor ausente o
        /// desconocido se asume <see cref="MissionDifficulty.Beginner"/> (multiplicador neutro ×1.0)
        /// para nunca bloquear la puntuación por un dato de contenido inesperado.
        /// </summary>
        public static MissionDifficulty FromString(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return MissionDifficulty.Beginner;

            var normalized = value.Trim();
            if (normalized.Equals("Intermediate", StringComparison.OrdinalIgnoreCase))
                return MissionDifficulty.Intermediate;
            if (normalized.Equals("Advanced", StringComparison.OrdinalIgnoreCase))
                return MissionDifficulty.Advanced;

            return MissionDifficulty.Beginner;
        }
    }
}
