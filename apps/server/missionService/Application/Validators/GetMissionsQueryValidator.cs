using FluentValidation;
using MissionService.Application.Queries;

namespace MissionService.Application.Validators
{
    /// <summary>
    /// Validates optional filter parameters for GetMissionsQuery.
    /// Applied automatically via the MediatR ValidationBehavior pipeline before the handler executes.
    /// </summary>
    public sealed class GetMissionsQueryValidator : AbstractValidator<GetMissionsQuery>
    {
        public GetMissionsQueryValidator()
        {
            // SearchTerm is optional; when provided, must not be blank and must be within length bounds
            RuleFor(x => x.SearchTerm)
                .Must(s => s is null || s.Trim().Length > 0)
                .WithMessage("SearchTerm must not be blank.")
                .MaximumLength(200)
                .WithMessage("SearchTerm cannot exceed 200 characters.");

            // Status is optional; when provided, must be one of the valid MissionStatus values
            RuleFor(x => x.Status)
                .Must(s => s is null ||
                           string.Equals(s.Trim(), "Active", System.StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(s.Trim(), "Inactive", System.StringComparison.OrdinalIgnoreCase))
                .WithMessage("Status must be 'Active' or 'Inactive'.");
        }
    }
}
