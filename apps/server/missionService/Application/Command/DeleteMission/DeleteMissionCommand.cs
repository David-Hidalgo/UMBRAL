using System;
using MediatR;

namespace MissionService.Application.Commands
{
    public sealed record DeleteMissionCommand(Guid Id) : IRequest<Unit>;
}
