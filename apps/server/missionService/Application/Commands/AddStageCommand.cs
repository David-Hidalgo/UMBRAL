using System;
using System.Collections.Generic;
using MediatR;

namespace MissionService.Application.Commands
{
    public sealed record AddStageCommand(
        Guid MissionId,
        string Name,
        string Description,
        int Order,
        List<CreateSubStageCommand> SubStages
    ) : IRequest<Guid>;
}
