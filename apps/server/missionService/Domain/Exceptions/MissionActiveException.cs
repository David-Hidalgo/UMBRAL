using System;

namespace MissionService.Domain.Exceptions
{
    public sealed class MissionActiveException : DomainException
    {
        public MissionActiveException(Guid missionId)
            : base($"Mission '{missionId}' is currently in use in active sessions and cannot be modified (RB-01).") { }
    }
}
