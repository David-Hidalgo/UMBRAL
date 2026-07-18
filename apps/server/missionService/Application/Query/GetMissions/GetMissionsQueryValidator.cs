using FluentValidation;
using MissionService.Application.Queries;

namespace MissionService.Application.Validators
{
    /// <summary>
    /// Valida los parámetros de filtrado opcionales para GetMissionsQuery.
    /// Se aplica automáticamente a través de la tubería (pipeline) MediatR ValidationBehavior antes de que se ejecute el handler.
    /// </summary>
    public sealed class GetMissionsQueryValidator : AbstractValidator<GetMissionsQuery>
    {
        public GetMissionsQueryValidator()
        {
            // SearchTerm es opcional; cuando se proporciona, no debe estar en blanco y debe estar dentro de los límites de longitud
            RuleFor(x => x.SearchTerm)
                .Must(s => s is null || s.Trim().Length > 0)
                .WithMessage("SearchTerm must not be blank.")
                .MaximumLength(200)
                .WithMessage("SearchTerm cannot exceed 200 characters.");

            // Status es opcional; cuando se proporciona, debe ser uno de los valores válidos de MissionStatus
            RuleFor(x => x.Status)
                .Must(s => s is null ||
                           string.Equals(s.Trim(), "Active", System.StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(s.Trim(), "Inactive", System.StringComparison.OrdinalIgnoreCase))
                .WithMessage("Status must be 'Active' or 'Inactive'.");
        }
    }
}
