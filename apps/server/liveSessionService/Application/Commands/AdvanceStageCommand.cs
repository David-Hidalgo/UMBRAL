using System;
using MediatR;

namespace LiveSessionService.Application.Commands
{
    public sealed record AdvanceStageCommand(
        Guid SessionId,
        int NextStageIndex,
        string JwtToken
    ) : IRequest<Unit>;
}
