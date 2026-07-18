using System;
using FluentValidation;
using MissionService.Application.Commands;

namespace MissionService.Application.Validators
{
    public sealed class UpdateMissionCommandValidator : AbstractValidator<UpdateMissionCommand>
    {
        public UpdateMissionCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Mission ID is required.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Mission name cannot be empty.")
                .MaximumLength(150).WithMessage("Mission name must not exceed 150 characters.")
                .When(x => x.Name != null);

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Mission description cannot be empty.")
                .MaximumLength(1000).WithMessage("Mission description must not exceed 1000 characters.")
                .When(x => x.Description != null);

            RuleFor(x => x.State)
                .NotEmpty().WithMessage("State cannot be empty.")
                .Must(state => state == "Active" || state == "Inactive")
                .WithMessage("State must be either 'Active' or 'Inactive'.")
                .When(x => x.State != null);

            RuleFor(x => x.DifficultyLevel)
                .NotEmpty().WithMessage("Difficulty level cannot be empty.")
                .Must(d => d == "Beginner" || d == "Intermediate" || d == "Advanced")
                .WithMessage("Difficulty level must be one of: Beginner, Intermediate, Advanced.")
                .When(x => x.DifficultyLevel != null);

            RuleFor(x => x.MaxTimeMinutes)
                .GreaterThan(0).WithMessage("Max time minutes must be greater than zero.")
                .When(x => x.MaxTimeMinutes != null);

            RuleFor(x => x)
                .Must(x => x.Name != null || x.Description != null || x.State != null || x.DifficultyLevel != null || x.MaxTimeMinutes != null)
                .WithMessage("At least one field to update must be provided.");
        }
    }
}
