using System.Collections.Generic;
using MediatR;
using MissionService.Application.DTOs;

namespace MissionService.Application.Queries
{
    /// <summary>
    /// CQRS query to retrieve a filtered list of missions.
    /// Both parameters are optional; when omitted, all missions are returned.
    /// Validated via MediatR ValidationBehavior before the handler executes.
    /// </summary>
    public sealed record GetMissionsQuery(
        string? SearchTerm,
        string? Status
    ) : IRequest<IEnumerable<MissionSummaryDto>>;
}
