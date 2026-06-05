using System;
using System.Linq;
using FluentValidation;
using MissionService.Application.Commands;

namespace MissionService.Application.Validators
{
    public sealed class CreateMissionCommandValidator : AbstractValidator<CreateMissionCommand>
    {
        public CreateMissionCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Mission name is required.")
                .MaximumLength(150).WithMessage("Mission name must not exceed 150 characters.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Mission description is required.")
                .MaximumLength(1000).WithMessage("Mission description must not exceed 1000 characters.");

            RuleFor(x => x.DifficultyLevel)
                .NotEmpty().WithMessage("Difficulty level is required.")
                .Must(d => d == "Beginner" || d == "Intermediate" || d == "Advanced")
                .WithMessage("Difficulty level must be one of: Beginner, Intermediate, Advanced.");

            RuleFor(x => x.MaxTimeMinutes)
                .GreaterThan(0).WithMessage("Max time minutes must be greater than zero.");

            RuleFor(x => x.Stages)
                .NotEmpty().WithMessage("Mission must contain at least one stage.");

            RuleForEach(x => x.Stages).SetValidator(new StageCommandValidator());
        }
    }

    internal sealed class StageCommandValidator : AbstractValidator<CreateStageCommand>
    {
        public StageCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Stage name is required.")
                .MaximumLength(150).WithMessage("Stage name must not exceed 150 characters.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Stage description is required.")
                .MaximumLength(500).WithMessage("Stage description must not exceed 500 characters.");

            RuleFor(x => x.Order)
                .GreaterThanOrEqualTo(0).WithMessage("Stage order must be greater than or equal to 0.");

            RuleFor(x => x.SubStages)
                .NotEmpty().WithMessage("Stage must contain at least one sub-stage.");

            RuleForEach(x => x.SubStages).SetValidator(new SubStageCommandValidator());
        }
    }

    internal sealed class SubStageCommandValidator : AbstractValidator<CreateSubStageCommand>
    {
        public SubStageCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Sub-stage title is required.")
                .MaximumLength(150).WithMessage("Sub-stage title must not exceed 150 characters.");

            RuleFor(x => x.Order)
                .GreaterThanOrEqualTo(0).WithMessage("Sub-stage order must be greater than or equal to 0.");

            RuleFor(x => x.Points)
                .GreaterThanOrEqualTo(0).WithMessage("Sub-stage points must be greater than or equal to 0.");

            RuleFor(x => x.Type)
                .NotEmpty().WithMessage("Sub-stage type is required.")
                .Must(type => type == "Trivia" || type == "TreasureHunt")
                .WithMessage("Sub-stage type must be either 'Trivia' or 'TreasureHunt'.");

            RuleForEach(x => x.Clues)
                .SetValidator(new ClueCommandValidator())
                .When(x => x.Clues != null);

            // Conditional validation for Trivia
            When(x => x.Type == "Trivia", () =>
            {
                RuleFor(x => x.Question)
                    .NotNull().WithMessage("Trivia sub-stage must have a question.");

                RuleFor(x => x.Question)
                    .SetValidator(new QuestionCommandValidator()!)
                    .When(x => x.Question != null);
            });

            // Conditional validation for Treasure Hunt
            When(x => x.Type == "TreasureHunt", () =>
            {
                RuleFor(x => x.Objective)
                    .NotEmpty().WithMessage("Treasure hunt objective is required.")
                    .MaximumLength(500).WithMessage("Treasure hunt objective must not exceed 500 characters.");

                RuleFor(x => x.SecretCode)
                    .NotEmpty().WithMessage("Treasure hunt secret code is required.")
                    .MaximumLength(100).WithMessage("Treasure hunt secret code must not exceed 100 characters.")
                    .Matches("^[a-zA-Z0-9-]+$").WithMessage("Treasure hunt secret code must be alphanumeric and can contain hyphens.");
            });
        }
    }

    internal sealed class QuestionCommandValidator : AbstractValidator<CreateQuestionCommand>
    {
        public QuestionCommandValidator()
        {
            RuleFor(x => x.Text)
                .NotEmpty().WithMessage("Question text is required.")
                .MaximumLength(500).WithMessage("Question text must not exceed 500 characters.");

            RuleFor(x => x.Answers)
                .NotEmpty().WithMessage("Question must contain at least one answer.")
                .Must(answers => answers != null && answers.Any(a => a.IsCorrect))
                .WithMessage("Question must have at least one correct answer marked.");

            RuleForEach(x => x.Answers).SetValidator(new AnswerCommandValidator());
        }
    }

    internal sealed class AnswerCommandValidator : AbstractValidator<CreateAnswerCommand>
    {
        public AnswerCommandValidator()
        {
            RuleFor(x => x.Text)
                .NotEmpty().WithMessage("Answer text is required.")
                .MaximumLength(200).WithMessage("Answer text must not exceed 200 characters.");
        }
    }

    internal sealed class ClueCommandValidator : AbstractValidator<CreateClueCommand>
    {
        public ClueCommandValidator()
        {
            RuleFor(x => x.Text)
                .NotEmpty().WithMessage("Clue text is required.")
                .MaximumLength(500).WithMessage("Clue text must not exceed 500 characters.");

            RuleFor(x => x.Order)
                .GreaterThanOrEqualTo(0).WithMessage("Clue order must be greater than or equal to 0.");
        }
    }
}
