using FluentValidation;
namespace UserService.Application.Command.CreateUser
{
    public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email format is invalid.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");

            RuleFor(x => x.Role)
                .Must(r => r is "admin" or "operator")
                .WithMessage("Role must be 'admin' or 'operator'. Players self-register via the mobile app.");
        }
    }
}
