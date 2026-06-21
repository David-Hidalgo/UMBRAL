using System;
using System.Collections.Generic;

namespace LiveSessionService.Application.DTOs
{
    public record LiveSessionDetailDto(
        Guid Id,
        Guid MissionId,
        string MissionName,
        string Name,
        DateTime? ScheduledAt,
        int CurrentStageIndex,
        string Status,
        IReadOnlyList<TeamDto> Teams
    );

    public record TeamDto(
        Guid Id,
        string Name,
        IReadOnlyList<PlayerDto> Players
    );

    public record PlayerDto(
        Guid Id,
        string KeycloakUserId,
        string DisplayName,
        DateTime JoinedAt
    );

    public record LiveSessionSummaryDto(
        Guid Id,
        Guid MissionId,
        string MissionName,
        string Name,
        DateTime? ScheduledAt,
        string Status,
        int TeamCount,
        int PlayerCount
    );
}
