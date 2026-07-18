using FluentValidation;
using ScoringService.Application.Commands;

namespace ScoringService.Application.Validators
{
    public sealed class ValidateEvidenceCommandValidator : AbstractValidator<ValidateEvidenceCommand>
    {
        public ValidateEvidenceCommandValidator()
        {
            RuleFor(x => x.SessionId)
                .NotEmpty().WithMessage("El ID de la sesión es requerido.");

            RuleFor(x => x.SubmissionId)
                .NotEmpty().WithMessage("El ID de la evidencia es requerido.");
        }
    }
}
