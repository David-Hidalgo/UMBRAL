using System;
using System.Collections.Generic;
using MediatR;

namespace MissionService.Application.Commands
{
    public sealed record ReorderStagesCommand(
        Guid MissionId,
        List<Guid> StageIdsInOrder
    ) : IRequest<Unit>;
}
