using System;

namespace MissionService.Domain.Exceptions
{
    public sealed class MissionActiveException : DomainException
    {
        public MissionActiveException(Guid missionId) 
            : base($"Mission '{missionId}' is active and its stages cannot be modified (RB-01).") { }
    }
}
