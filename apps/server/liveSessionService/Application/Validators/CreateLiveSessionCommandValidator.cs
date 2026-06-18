using FluentValidation;
using LiveSessionService.Application.Commands;

namespace LiveSessionService.Application.Validators
{
    public sealed class CreateLiveSessionCommandValidator : AbstractValidator<CreateLiveSessionCommand>
    {
        public CreateLiveSessionCommandValidator()
        {
            RuleFor(x => x.MissionId)
                .NotEmpty().WithMessage("El ID de la misión es requerido.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre de la sesión es requerido.")
                .MaximumLength(100).WithMessage("El nombre de la sesión no debe exceder los 100 caracteres.");

            RuleFor(x => x.JwtToken)
                .NotEmpty().WithMessage("El token JWT es requerido.");
        }
    }
}
