using System;

namespace MissionService.Domain.Exceptions
{
    public sealed class CannotRemoveLastStageException : DomainException
    {
        public CannotRemoveLastStageException(Guid missionId)
            : base($"Cannot remove the last stage of mission with ID '{missionId}'.")
        {
        }
    }
}
