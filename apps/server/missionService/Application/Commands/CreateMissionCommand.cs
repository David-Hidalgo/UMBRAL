using System;
using System.Collections.Generic;
using MediatR;

namespace MissionService.Application.Commands
{
    public sealed record CreateMissionCommand(
        string Name,
        string Description,
        string DifficultyLevel,
        int MaxTimeMinutes,
        List<CreateStageCommand> Stages
    ) : IRequest<Guid>;

    public sealed record CreateStageCommand(
        string Name,
        string Description,
        int Order,
        List<CreateSubStageCommand> SubStages
    );

    public sealed record CreateSubStageCommand(
        string Title,
        int Order,
        string Type, // "Trivia" o "TreasureHunt"
        int Points,
        // Campos para Trivia (exactamente 1 pregunta)
        CreateQuestionCommand? Question = null,
        // Campos para TreasureHunt
        string? Objective = null,
        string? SecretCode = null,
        List<CreateClueCommand>? Clues = null,
        Guid? Id = null
    );

    public sealed record CreateQuestionCommand(
        string Text,
        List<CreateAnswerCommand> Answers,
        Guid? Id = null
    );

    public sealed record CreateAnswerCommand(
        string Text,
        bool IsCorrect,
        Guid? Id = null
    );

    public sealed record CreateClueCommand(
        string Text,
        int Order,
        Guid? Id = null
    );
}
