using FluentValidation;
using LiveSessionService.Application.Commands;

namespace LiveSessionService.Application.Validators
{
    public sealed class AdvanceStageCommandValidator : AbstractValidator<AdvanceStageCommand>
    {
        public AdvanceStageCommandValidator()
        {
            RuleFor(x => x.SessionId)
                .NotEmpty().WithMessage("El ID de la sesión es requerido.");

            RuleFor(x => x.NextStageIndex)
                .GreaterThanOrEqualTo(0).WithMessage("El índice de la etapa debe ser mayor o igual a 0.");

            RuleFor(x => x.JwtToken)
                .NotEmpty().WithMessage("El token JWT es requerido.");
        }
    }
}
