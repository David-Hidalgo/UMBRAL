using System;
using System.Collections.Generic;
using MediatR;

namespace MissionService.Application.Commands
{
    public sealed record UpdateStageCommand(
        Guid MissionId,
        Guid StageId,
        string Name,
        string Description,
        List<CreateSubStageCommand> SubStages
    ) : IRequest<Unit>;
}
