using System;
using MediatR;

namespace LiveSessionService.Application.Commands
{
    public sealed record CreateTeamCommand(Guid SessionId, string TeamName) : IRequest<Guid>;
}
