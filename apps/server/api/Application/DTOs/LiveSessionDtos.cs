using System;
using System.Collections.Generic;

namespace api.Application.DTOs
{
    public record CreateLiveSessionRequest(Guid MissionId, string Name, DateTime? ScheduledAt);
    public record TransitionStatusRequest(string TargetStatus);
    public record CreateTeamRequest(string TeamName);
    public record AdvanceStageRequest(int NextStageIndex);

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
