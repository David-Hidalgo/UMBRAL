using System.Collections.Generic;
using MediatR;

namespace MissionService.Application.Commands
{
    public sealed record CreateMissionCommand(
        string Name,
        string Description,
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
        CreateQuestionCommand? Question,
        // Campos para TreasureHunt
        string? Objective,
        List<CreateClueCommand>? Clues
    );

    public sealed record CreateQuestionCommand(
        string Text,
        List<CreateAnswerCommand> Answers
    );

    public sealed record CreateAnswerCommand(
        string Text,
        bool IsCorrect
    );

    public sealed record CreateClueCommand(
        string Text,
        int Order,
        int PenaltyPoints
    );
}
