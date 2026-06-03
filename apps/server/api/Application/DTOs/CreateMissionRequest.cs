using System.Collections.Generic;

namespace api.Application.DTOs;

public sealed record CreateMissionRequest(
    string Name,
    string Description,
    List<CreateStageRequest> Stages
);

public sealed record CreateStageRequest(
    string Name,
    string Description,
    int Order,
    List<CreateSubStageRequest> SubStages
);

public sealed record CreateSubStageRequest(
    string Title,
    int Order,
    string Type, // "Trivia" o "TreasureHunt"
    int Points,
    CreateQuestionRequest? Question,
    string? Objective,
    string? SecretCode,
    List<CreateClueRequest>? Clues
);

public sealed record CreateQuestionRequest(
    string Text,
    List<CreateAnswerRequest> Answers
);

public sealed record CreateAnswerRequest(
    string Text,
    bool IsCorrect
);

public sealed record CreateClueRequest(
    string Text,
    int Order
);
