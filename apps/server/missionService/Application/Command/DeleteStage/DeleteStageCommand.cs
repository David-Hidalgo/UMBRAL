using System;
using MediatR;

namespace MissionService.Application.Commands
{
    public sealed record DeleteStageCommand(Guid MissionId, Guid StageId) : IRequest<Unit>;
}
