using System;
using MediatR;

namespace LiveSessionService.Application.Commands
{
    public sealed record JoinTeamCommand(
        Guid SessionId,
        Guid TeamId,
        string KeycloakUserId,
        string DisplayName
    ) : IRequest<Unit>;
}
