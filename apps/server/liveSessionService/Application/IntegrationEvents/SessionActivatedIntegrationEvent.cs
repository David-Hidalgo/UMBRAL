using System;
using System.Collections.Generic;

namespace LiveSessionService.Application.IntegrationEvents
{
    public record SessionActivatedIntegrationEvent(Guid SessionId, IReadOnlyList<Guid> SubStageIds);
}
