using System;
using System.Collections.Generic;

namespace LiveSessionService.Application.IntegrationEvents
{
    public record StageTransitionedIntegrationEvent(Guid SessionId, IReadOnlyList<Guid> PreviousSubStageIds, IReadOnlyList<Guid> NewSubStageIds);
}
