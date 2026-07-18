using FluentValidation;
using ScoringService.Application.Commands;

namespace ScoringService.Application.Validators
{
    public sealed class SubmitEvidenceCommandValidator : AbstractValidator<SubmitEvidenceCommand>
    {
        public SubmitEvidenceCommandValidator()
        {
            RuleFor(x => x.SessionId)
                .NotEmpty().WithMessage("El ID de la sesión es requerido.");

            RuleFor(x => x.TeamId)
                .NotEmpty().WithMessage("El ID del equipo es requerido.");

            RuleFor(x => x.SubStageId)
                .NotEmpty().WithMessage("El ID de la etapa es requerido.");

            RuleFor(x => x.Type)
                .IsInEnum().WithMessage("El tipo de evidencia no es válido.");

            RuleFor(x => x.Payload)
                .NotEmpty().WithMessage("La respuesta o evidencia no puede estar vacía.")
                .MaximumLength(500).WithMessage("La evidencia no debe exceder los 500 caracteres.");
        }
    }
}
