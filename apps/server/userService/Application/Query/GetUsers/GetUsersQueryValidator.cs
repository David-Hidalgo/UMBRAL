using FluentValidation;
namespace UserService.Application.Query.GetUsers
{
    /// <summary>
    /// Validates filter parameters forwarded to Keycloak before the Handler executes.
    /// Applied automatically via the MediatR ValidationBehavior pipeline.
    /// Role values must be lowercase (consistent with CreateUserCommandValidator).
    /// </summary>
    public sealed class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
    {
        public GetUsersQueryValidator()
        {
            // SearchTerm is optional but must not be whitespace-only if provided
            RuleFor(x => x.SearchTerm)
                .Must(s => s is null || s.Trim().Length > 0)
                .WithMessage("SearchTerm must not be blank.")
                .MaximumLength(200)
                .WithMessage("SearchTerm cannot exceed 200 characters.");

            // Role is optional; when provided it must be one of the three exact lowercase app roles
            RuleFor(x => x.Role)
                .Must(r => r is null or "admin" or "operator" or "player")
                .WithMessage("Role must be 'admin', 'operator', or 'player'.");
        }
    }
}
