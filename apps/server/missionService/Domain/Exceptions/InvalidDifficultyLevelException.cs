using System;

namespace MissionService.Domain.Exceptions
{
    public sealed class InvalidDifficultyLevelException : DomainException
    {
        public InvalidDifficultyLevelException(string value)
            : base($"Invalid difficulty level value: '{value}'. Valid values are: Beginner, Intermediate, Advanced.")
        {
        }
    }
}
