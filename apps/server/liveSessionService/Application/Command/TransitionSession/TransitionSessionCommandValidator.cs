using FluentValidation;
using LiveSessionService.Application.Commands;

namespace LiveSessionService.Application.Validators
{
    public sealed class TransitionSessionCommandValidator : AbstractValidator<TransitionSessionCommand>
    {
        public TransitionSessionCommandValidator()
        {
            RuleFor(x => x.SessionId)
                .NotEmpty().WithMessage("El ID de la sesión es requerido.");

            RuleFor(x => x.TargetStatus)
                .IsInEnum().WithMessage("El estado destino no es válido.");

            RuleFor(x => x.JwtToken)
                .NotEmpty().WithMessage("El token JWT es requerido.");
        }
    }
}
