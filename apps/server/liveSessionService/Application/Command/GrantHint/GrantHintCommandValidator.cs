using FluentValidation;
using LiveSessionService.Application.Commands;

namespace LiveSessionService.Application.Validators
{
    public sealed class GrantHintCommandValidator : AbstractValidator<GrantHintCommand>
    {
        public GrantHintCommandValidator()
        {
            RuleFor(x => x.SessionId)
                .NotEmpty().WithMessage("El ID de la sesión es requerido.");

            RuleFor(x => x.TeamId)
                .NotEmpty().WithMessage("El ID del equipo es requerido.");

            RuleFor(x => x.SubStageId)
                .NotEmpty().WithMessage("El ID de la subetapa es requerido.");

            RuleFor(x => x.ClueId)
                .NotEmpty().WithMessage("El ID de la pista es requerido.");
        }
    }
}
