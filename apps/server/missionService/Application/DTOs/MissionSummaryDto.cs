using System;

namespace MissionService.Application.DTOs
{
    public sealed record MissionSummaryDto(
        Guid Id,
        string Name,
        string Description,
        string State,
        int ActiveSessionCount,
        string DifficultyLevel,
        int MaxTimeMinutes,
        int StageCount
    );
}
