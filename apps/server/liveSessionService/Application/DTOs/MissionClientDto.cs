using System;
using System.Collections.Generic;

namespace LiveSessionService.Application.DTOs
{
    public record MissionClientDto(
        Guid Id,
        string Name,
        string State,
        int MaxTimeMinutes,
        IReadOnlyList<StageClientDto> Stages
    );

    public record StageClientDto(
        Guid Id,
        string Name,
        string Description,
        int Order,
        IReadOnlyList<SubStageClientDto> SubStages
    );

    public record SubStageClientDto(
        Guid Id,
        string Title,
        int Order,
        int Points
    );
}
