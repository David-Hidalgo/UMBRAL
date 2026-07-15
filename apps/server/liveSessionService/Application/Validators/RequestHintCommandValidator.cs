using FluentValidation;
using LiveSessionService.Application.Commands;

namespace LiveSessionService.Application.Validators
{
    public sealed class RequestHintCommandValidator : AbstractValidator<RequestHintCommand>
    {
        public RequestHintCommandValidator()
        {
            RuleFor(x => x.SessionId)
                .NotEmpty().WithMessage("El ID de la sesión es requerido.");

            RuleFor(x => x.TeamId)
                .NotEmpty().WithMessage("El ID del equipo es requerido.");

            RuleFor(x => x.SubStageId)
                .NotEmpty().WithMessage("El ID de la subetapa es requerido.");

            RuleFor(x => x.KeycloakUserId)
                .NotEmpty().WithMessage("El ID de Keycloak del usuario es requerido.");
        }
    }
}
