using System;

namespace MissionService.Domain.Exceptions
{
    public sealed class MissionHasActiveSessionsException : DomainException
    {
        public MissionHasActiveSessionsException(Guid missionId)
            : base($"Mission '{missionId}' cannot be deactivated or deleted because it or its stages have active sessions.") { }
    }
}
