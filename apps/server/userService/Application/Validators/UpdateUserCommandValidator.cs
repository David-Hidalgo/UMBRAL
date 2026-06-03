using FluentValidation;
using UserService.Application.Commands;

namespace UserService.Application.Validators
{
    public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
    {
        public UpdateUserCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("User ID is required.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");

            RuleFor(x => x.Role)
                .Must(r => r is "admin" or "operator" or "player")
                .WithMessage("Role must be 'admin', 'operator', or 'player'.");
        }
    }
}
