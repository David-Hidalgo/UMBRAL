using FluentValidation;
using MissionService.Application.Queries;

namespace MissionService.Application.Validators;

public sealed class GetMissionByIdQueryValidator : AbstractValidator<GetMissionByIdQuery>
{
    public GetMissionByIdQueryValidator()
    {
        RuleFor(q => q.Id)
            .NotEmpty()
            .WithMessage("Mission ID is required.");
    }
}
