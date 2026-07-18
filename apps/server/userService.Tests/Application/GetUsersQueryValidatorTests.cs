using FluentAssertions;
using UserService.Application.Query.GetUsers;
using Xunit;

namespace UserService.Tests.Application;

public class GetUsersQueryValidatorTests
{
    private readonly GetUsersQueryValidator _sut = new();

    // ── Happy paths ──────────────────────────────────────────────────────────

    [Fact]
    public void Validate_WithNullSearchTermAndNullRole_IsValid()
    {
        // Arrange
        var query = new GetUsersQuery(null, null);

        // Act
        var result = _sut.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("operator")]
    [InlineData("player")]
    public void Validate_WithValidRole_IsValid(string role)
    {
        // Arrange
        var query = new GetUsersQuery(null, role);

        // Act
        var result = _sut.Validate(query);

        // Assert
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(GetUsersQuery.Role));
    }

    [Fact]
    public void Validate_WithValidSearchTermAndRole_IsValid()
    {
        // Arrange
        var query = new GetUsersQuery("ana", "player");

        // Act
        var result = _sut.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    // ── Role validation ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("superadmin")]
    [InlineData("moderator")]
    [InlineData("ADMIN")]
    [InlineData("unknown")]
    public void Validate_WithInvalidRole_ReturnsRoleError(string role)
    {
        // Arrange
        var query = new GetUsersQuery(null, role);

        // Act
        var result = _sut.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetUsersQuery.Role));
    }

    // ── SearchTerm validation ─────────────────────────────────────────────────

    [Fact]
    public void Validate_WithWhitespaceOnlySearchTerm_ReturnsSearchTermError()
    {
        // Arrange
        var query = new GetUsersQuery("   ", null);

        // Act
        var result = _sut.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetUsersQuery.SearchTerm));
    }

    [Fact]
    public void Validate_WithSearchTermExceeding200Chars_ReturnsSearchTermError()
    {
        // Arrange
        var longTerm = new string('x', 201);
        var query = new GetUsersQuery(longTerm, null);

        // Act
        var result = _sut.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetUsersQuery.SearchTerm));
    }

    [Fact]
    public void Validate_WithSearchTermOf200Chars_IsValid()
    {
        // Arrange
        var maxTerm = new string('x', 200);
        var query = new GetUsersQuery(maxTerm, null);

        // Act
        var result = _sut.Validate(query);

        // Assert
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(GetUsersQuery.SearchTerm));
    }
}
