using System;
using FluentValidation;
using MissionService.Application.Commands;

namespace MissionService.Application.Validators
{
    public sealed class UpdateStageCommandValidator : AbstractValidator<UpdateStageCommand>
    {
        public UpdateStageCommandValidator()
        {
            RuleFor(x => x.MissionId)
                .NotEmpty().WithMessage("Mission ID is required.");

            RuleFor(x => x.StageId)
                .NotEmpty().WithMessage("Stage ID is required.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Stage name is required.")
                .MaximumLength(150).WithMessage("Stage name must not exceed 150 characters.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Stage description is required.")
                .MaximumLength(500).WithMessage("Stage description must not exceed 500 characters.");

            RuleFor(x => x.SubStages)
                .NotEmpty().WithMessage("Stage must contain at least one sub-stage.");

            RuleForEach(x => x.SubStages).SetValidator(new SubStageCommandValidator());
        }
    }
}
