using FluentValidation;
using UserService.Application.Commands;

namespace UserService.Application.Validators;

public sealed class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(x => x.KeycloakUserId)
            .NotEmpty()
            .WithMessage("Keycloak user ID is required.");
    }
}
