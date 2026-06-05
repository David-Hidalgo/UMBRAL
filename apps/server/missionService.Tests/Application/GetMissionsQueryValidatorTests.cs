using FluentAssertions;
using MissionService.Application.Queries;
using MissionService.Application.Validators;
using Xunit;

namespace MissionService.Tests.Application
{
    public class GetMissionsQueryValidatorTests
    {
        private readonly GetMissionsQueryValidator _validator = new();

        [Fact]
        public void Should_Be_Valid_When_Both_Filters_Are_Null()
        {
            // Arrange
            var query = new GetMissionsQuery(null, null);

            // Act
            var result = _validator.Validate(query);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Should_Be_Valid_When_SearchTerm_Is_A_Normal_String()
        {
            // Arrange
            var query = new GetMissionsQuery("treasure", null);

            // Act
            var result = _validator.Validate(query);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Should_Be_Invalid_When_SearchTerm_Is_Whitespace_Only()
        {
            // Arrange
            var query = new GetMissionsQuery("   ", null);

            // Act
            var result = _validator.Validate(query);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "SearchTerm");
        }

        [Fact]
        public void Should_Be_Invalid_When_SearchTerm_Exceeds_200_Characters()
        {
            // Arrange
            var longTerm = new string('x', 201);
            var query = new GetMissionsQuery(longTerm, null);

            // Act
            var result = _validator.Validate(query);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "SearchTerm");
        }

        [Fact]
        public void Should_Be_Valid_When_SearchTerm_Is_Exactly_200_Characters()
        {
            // Arrange
            var maxTerm = new string('a', 200);
            var query = new GetMissionsQuery(maxTerm, null);

            // Act
            var result = _validator.Validate(query);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("Active")]
        [InlineData("Inactive")]
        [InlineData("active")]
        [InlineData("inactive")]
        [InlineData("ACTIVE")]
        [InlineData("INACTIVE")]
        public void Should_Be_Valid_When_Status_Is_A_Valid_Value(string status)
        {
            // Arrange
            var query = new GetMissionsQuery(null, status);

            // Act
            var result = _validator.Validate(query);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("Pending")]
        [InlineData("Deleted")]
        [InlineData("1")]
        [InlineData("")]
        public void Should_Be_Invalid_When_Status_Is_An_Unknown_Value(string status)
        {
            // Arrange
            var query = new GetMissionsQuery(null, status);

            // Act
            var result = _validator.Validate(query);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Status");
        }

        [Fact]
        public void Should_Be_Valid_When_Both_Filters_Are_Provided_And_Valid()
        {
            // Arrange
            var query = new GetMissionsQuery("mission alpha", "Active");

            // Act
            var result = _validator.Validate(query);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Should_Report_Both_Errors_When_Both_Filters_Are_Invalid()
        {
            // Arrange
            var query = new GetMissionsQuery("   ", "Unknown");

            // Act
            var result = _validator.Validate(query);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "SearchTerm");
            result.Errors.Should().Contain(e => e.PropertyName == "Status");
        }
    }
}
