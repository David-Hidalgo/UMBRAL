using FluentValidation;
namespace UserService.Application.Command.DeleteUser;

public sealed class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(x => x.KeycloakUserId)
            .NotEmpty()
            .WithMessage("Keycloak user ID is required.");
    }
}
