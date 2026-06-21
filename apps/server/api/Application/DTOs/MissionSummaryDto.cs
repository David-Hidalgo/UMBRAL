using System;

namespace api.Application.DTOs;

/// <summary>
/// DTO espejo utilizado por el API Gateway para deserializar la lista de misiones
/// devuelta por el MissionService ascendente (upstream).
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
