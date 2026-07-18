using FluentAssertions;
using UserService.Application.Command.DeleteUser;
using Xunit;

namespace UserService.Tests.Application;

public class DeleteUserCommandValidatorTests
{
    private readonly DeleteUserCommandValidator _sut = new();

    // ─── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public void Validate_WithValidKeycloakUserId_ReturnsNoErrors()
    {
        // Arrange
        var command = new DeleteUserCommand("kc-abc-123");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("some-guid-id")]
    [InlineData("123e4567-e89b-12d3-a456-426614174000")]
    [InlineData("any-non-empty-string")]
    public void Validate_WithNonEmptyKeycloakUserId_IsValid(string keycloakUserId)
    {
        // Arrange
        var command = new DeleteUserCommand(keycloakUserId);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    // ─── Sad path ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyOrWhitespaceKeycloakUserId_ReturnsError(string keycloakUserId)
    {
        // Arrange
        var command = new DeleteUserCommand(keycloakUserId);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DeleteUserCommand.KeycloakUserId));
    }

    [Fact]
    public void Validate_WithEmptyKeycloakUserId_ErrorMessageMentionsRequired()
    {
        // Arrange
        var command = new DeleteUserCommand(string.Empty);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(DeleteUserCommand.KeycloakUserId) &&
            e.ErrorMessage.Contains("required"));
    }
}
