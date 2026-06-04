using System;
using FluentAssertions;
using MissionService.Application.Queries;
using MissionService.Application.Validators;
using Xunit;

namespace MissionService.Tests.Application
{
    public class GetMissionByIdQueryValidatorTests
    {
        private readonly GetMissionByIdQueryValidator _validator = new();

        [Fact]
        public void Validate_Should_Pass_When_Id_Is_Valid_Guid()
        {
            // Arrange
            var query = new GetMissionByIdQuery(Guid.NewGuid());

            // Act
            var result = _validator.Validate(query);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_Should_Fail_When_Id_Is_Empty_Guid()
        {
            // Arrange
            var query = new GetMissionByIdQuery(Guid.Empty);

            // Act
            var result = _validator.Validate(query);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e =>
                e.PropertyName == nameof(GetMissionByIdQuery.Id) &&
                e.ErrorMessage == "Mission ID is required.");
        }
    }
}
