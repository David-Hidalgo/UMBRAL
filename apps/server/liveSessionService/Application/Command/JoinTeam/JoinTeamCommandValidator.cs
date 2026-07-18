using FluentValidation;
using LiveSessionService.Application.Commands;

namespace LiveSessionService.Application.Validators
{
    public sealed class JoinTeamCommandValidator : AbstractValidator<JoinTeamCommand>
    {
        public JoinTeamCommandValidator()
        {
            RuleFor(x => x.SessionId)
                .NotEmpty().WithMessage("El ID de la sesión es requerido.");

            RuleFor(x => x.TeamId)
                .NotEmpty().WithMessage("El ID del equipo es requerido.");

            RuleFor(x => x.KeycloakUserId)
                .NotEmpty().WithMessage("El ID de Keycloak del usuario es requerido.");

            RuleFor(x => x.DisplayName)
                .NotEmpty().WithMessage("El nombre visible es requerido.")
                .MaximumLength(100).WithMessage("El nombre visible no debe exceder los 100 caracteres.");
        }
    }
}
