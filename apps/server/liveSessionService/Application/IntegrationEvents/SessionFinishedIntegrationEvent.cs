using System;
using System.Collections.Generic;

namespace LiveSessionService.Application.IntegrationEvents
{
    public record SessionFinishedIntegrationEvent(Guid SessionId, IReadOnlyList<Guid> SubStageIds);
}
