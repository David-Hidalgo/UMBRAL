using System;
using System.Collections.Generic;

namespace MissionService.Application.DTOs
{
    public sealed record MissionDetailDto(
        Guid Id,
        string Name,
        string Description,
        string State,
        int ActiveSessionCount,
        string DifficultyLevel,
        int MaxTimeMinutes,
        IReadOnlyList<StageDetailDto> Stages
    );

    public sealed record StageDetailDto(
        Guid Id,
        string Name,
        string Description,
        int Order,
        int ActiveSessionCount,
        IReadOnlyList<SubStageDetailDto> SubStages
    );

    public sealed record SubStageDetailDto(
        Guid Id,
        string Title,
        int Order,
        int Points,
        int ActiveSessionCount,
        QuestionDetailDto? Question,
        string? Objective,
        string? SecretCode,
        IReadOnlyList<ClueDetailDto>? Clues,
        double? Latitude = null,
        double? Longitude = null,
        int? SearchRadiusMeters = null
    );

    public sealed record QuestionDetailDto(Guid Id, string Text, IReadOnlyList<AnswerDetailDto> Answers);
    public sealed record AnswerDetailDto(Guid Id, string Text, bool IsCorrect);
    public sealed record ClueDetailDto(Guid Id, string Text, int Order);
}
