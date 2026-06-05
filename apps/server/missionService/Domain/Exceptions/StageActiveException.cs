using System;

namespace MissionService.Domain.Exceptions
{
    public sealed class StageActiveException : DomainException
    {
        public StageActiveException(Guid stageId)
            : base($"Stage '{stageId}' is active in one or more sessions and cannot be modified or deleted (RB-02).") { }
    }
}
