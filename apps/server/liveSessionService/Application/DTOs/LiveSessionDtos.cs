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
        IReadOnlyList<TeamDto> Teams,
        int MaxTimeMinutes,
        DateTime? EndsAt,
        int? RemainingSecondsOnPause
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

    public record HintRequestDto(
        Guid Id,
        Guid TeamId,
        string TeamName,
        Guid SubStageId,
        string Status,
        DateTime RequestedAt,
        DateTime? ResolvedAt
    );

    public record ReleasedHintDto(
        Guid Id,
        Guid TeamId,
        Guid SubStageId,
        Guid ClueId,
        string Text,
        int Order,
        DateTime ReleasedAt
    );
}
