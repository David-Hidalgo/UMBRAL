using System;

namespace MissionService.Application.DTOs
{
    /// <summary>
    /// Read-side DTO for the mission list. Intentionally omits the full Stage/SubStage
    /// hierarchy to keep list responses lightweight (CQRS read model).
    /// </summary>
    public sealed record MissionSummaryDto(
        Guid Id,
        string Name,
        string Description,
        string Status,
        int StageCount
    );
}
