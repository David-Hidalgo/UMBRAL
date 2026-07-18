using System;
using System.Collections.Generic;
using LiveSessionService.Application.DTOs;
using MediatR;

namespace LiveSessionService.Application.Queries
{
    public sealed record GetLiveSessionsQuery(
        Guid? MissionId = null,
        string? Status = null
    ) : IRequest<IEnumerable<LiveSessionSummaryDto>>;
}
