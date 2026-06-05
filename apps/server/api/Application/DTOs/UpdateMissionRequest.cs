namespace api.Application.DTOs;

public sealed record UpdateMissionRequest(
    string? Name,
    string? Description,
    string? State,
    string? DifficultyLevel,
    int? MaxTimeMinutes
);
