using System;

namespace MissionService.Domain.Exceptions
{
    public sealed class StageNotFoundException : DomainException
    {
        public StageNotFoundException(Guid stageId)
            : base($"Stage with ID '{stageId}' was not found.") { }
    }
}
