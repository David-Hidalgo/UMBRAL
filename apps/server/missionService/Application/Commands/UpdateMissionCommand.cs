using System;
using MediatR;

namespace MissionService.Application.Commands
{
    public sealed record UpdateMissionCommand(
        Guid Id,
        string? Name,
        string? Description,
        string? State, // "Active" o "Inactive"
        string? DifficultyLevel, // "Beginner", "Intermediate", "Advanced"
        int? MaxTimeMinutes
    ) : IRequest<Unit>;
}
