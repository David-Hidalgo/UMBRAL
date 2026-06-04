namespace MissionService.Application.DTOs;

public sealed record MissionDetailDto(
    Guid Id,
    string Name,
    string Description,
    string Status,
    IReadOnlyList<StageDetailDto> Stages
);

public sealed record StageDetailDto(
    Guid Id,
    string Name,
    string Description,
    int Order,
    IReadOnlyList<SubStageDetailDto> SubStages
);

public sealed record SubStageDetailDto(
    Guid Id,
    string Title,
    int Order,
    int Points,
    QuestionDetailDto? Question,
    string? Objective,
    string? SecretCode,
    IReadOnlyList<ClueDetailDto>? Clues
);

public sealed record QuestionDetailDto(
    Guid Id,
    string Text,
    IReadOnlyList<AnswerDetailDto> Answers
);

public sealed record AnswerDetailDto(
    Guid Id,
    string Text,
    bool IsCorrect
);

public sealed record ClueDetailDto(
    Guid Id,
    string Text,
    int Order
);
