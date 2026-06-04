using System;

namespace api.Application.DTOs;

/// <summary>
/// Mirror DTO used by the API Gateway to deserialize the mission list
/// returned by the MissionService upstream.
/// </summary>
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
