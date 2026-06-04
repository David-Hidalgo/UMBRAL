using System;
using MissionService.Domain.Exceptions;

namespace MissionService.Domain.Enums
{
    public enum DifficultyLevel
    {
        Beginner,
        Intermediate,
        Advanced
    }

    public static class DifficultyLevelExtensions
    {
        public static DifficultyLevel FromString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Difficulty level value cannot be empty.", nameof(value));

            var normalized = value.Trim();
            if (normalized.Equals("Beginner", StringComparison.OrdinalIgnoreCase))
                return DifficultyLevel.Beginner;
            if (normalized.Equals("Intermediate", StringComparison.OrdinalIgnoreCase))
                return DifficultyLevel.Intermediate;
            if (normalized.Equals("Advanced", StringComparison.OrdinalIgnoreCase))
                return DifficultyLevel.Advanced;

            throw new InvalidDifficultyLevelException(value);
        }
    }
}
