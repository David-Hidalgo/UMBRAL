using FluentValidation;
using ScoringService.Application.Commands;

namespace ScoringService.Application.Validators
{
    public sealed class ApplyPenaltyCommandValidator : AbstractValidator<ApplyPenaltyCommand>
    {
        public ApplyPenaltyCommandValidator()
        {
            RuleFor(x => x.SessionId)
                .NotEmpty().WithMessage("El ID de la sesión es requerido.");

            RuleFor(x => x.TeamId)
                .NotEmpty().WithMessage("El ID del equipo es requerido.");

            RuleFor(x => x.Points)
                .GreaterThan(0).WithMessage("La penalización debe restar una cantidad positiva de puntos.");

            // RB-06: el motivo de la penalización es obligatorio.
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("El motivo de la penalización es obligatorio.")
                .MaximumLength(300).WithMessage("El motivo no debe exceder los 300 caracteres.");
        }
    }
}
