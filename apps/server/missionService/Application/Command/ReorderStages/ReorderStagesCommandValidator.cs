using System;
using FluentValidation;
using MissionService.Application.Commands;

namespace MissionService.Application.Validators
{
    public sealed class ReorderStagesCommandValidator : AbstractValidator<ReorderStagesCommand>
    {
        public ReorderStagesCommandValidator()
        {
            RuleFor(x => x.MissionId)
                .NotEmpty().WithMessage("Mission ID is required.");

            RuleFor(x => x.StageIdsInOrder)
                .NotEmpty().WithMessage("Stage IDs list is required and cannot be empty.")
                .Must(list => list != null && !list.Contains(Guid.Empty))
                .WithMessage("Stage IDs list must not contain empty GUIDs.");
        }
    }
}
