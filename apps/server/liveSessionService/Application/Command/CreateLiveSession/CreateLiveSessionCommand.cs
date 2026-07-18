using System;
using MediatR;

namespace LiveSessionService.Application.Commands
{
    public sealed record CreateLiveSessionCommand(
        Guid MissionId,
        string Name,
        DateTime? ScheduledAt,
        string JwtToken
    ) : IRequest<Guid>;
}
