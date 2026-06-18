using System;
using System.Collections.Generic;

namespace LiveSessionService.Application.IntegrationEvents
{
    public record SessionCancelledIntegrationEvent(Guid SessionId, IReadOnlyList<Guid> SubStageIds);
}
