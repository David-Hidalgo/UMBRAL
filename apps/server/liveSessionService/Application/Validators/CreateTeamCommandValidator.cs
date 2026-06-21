using FluentValidation;
using LiveSessionService.Application.Commands;

namespace LiveSessionService.Application.Validators
{
    public sealed class CreateTeamCommandValidator : AbstractValidator<CreateTeamCommand>
    {
        public CreateTeamCommandValidator()
        {
            RuleFor(x => x.SessionId)
                .NotEmpty().WithMessage("El ID de la sesión es requerido.");

            RuleFor(x => x.TeamName)
                .NotEmpty().WithMessage("El nombre del equipo es requerido.")
                .MaximumLength(50).WithMessage("El nombre del equipo no debe exceder los 50 caracteres.");
        }
    }
}
