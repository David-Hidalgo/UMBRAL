using System.Collections.Generic;

namespace MissionService.Application.DTOs
{
    public sealed record CreateMissionRequest(
        string Name,
        string Description,
        string DifficultyLevel,
        int MaxTimeMinutes,
        List<CreateStageRequest> Stages
    );

    public sealed record CreateStageRequest(
        string Name,
        string Description,
        int Order,
        List<CreateSubStageRequest> SubStages
    );

    public sealed record CreateSubStageRequest(
        Guid? Id,
        string Title,
        int Order,
        string Type, // "Trivia" o "TreasureHunt"
        int Points,
        // Trivia: exactamente 1 pregunta
        CreateQuestionRequest? Question,
        // TreasureHunt
        string? Objective,
        string? SecretCode,
        List<CreateClueRequest>? Clues,
        // Ubicación del tesoro (opcional)
        double? Latitude = null,
        double? Longitude = null,
        int? SearchRadiusMeters = null
    );

    public sealed record CreateQuestionRequest(
        Guid? Id,
        string Text,
        List<CreateAnswerRequest> Answers
    );

    public sealed record CreateAnswerRequest(
        Guid? Id,
        string Text,
        bool IsCorrect
    );

    public sealed record CreateClueRequest(
        Guid? Id,
        string Text,
        int Order
    );
}
