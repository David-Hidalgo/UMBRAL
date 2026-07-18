using System;
using LiveSessionService.Domain.Enums;
using MediatR;

namespace LiveSessionService.Application.Commands
{
    public sealed record TransitionSessionCommand(
        Guid SessionId,
        LiveSessionStatus TargetStatus,
        string JwtToken
    ) : IRequest<Unit>;
}
