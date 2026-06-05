using FluentAssertions;
using UserService.Application.Commands;
using UserService.Application.Validators;
using Xunit;

namespace UserService.Tests.Application;

public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _sut = new();

    // ─── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        // Arrange
        var command = new CreateUserCommand("valid@umbral.io", "Valid User", "Password123!");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    // ─── Email validation ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyEmail_ReturnsEmailError(string email)
    {
        // Arrange
        var command = new CreateUserCommand(email, "Valid User", "Password123!");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserCommand.Email));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("plainaddress")]
    public void Validate_WithInvalidEmailFormat_ReturnsEmailError(string email)
    {
        // Arrange
        var command = new CreateUserCommand(email, "Valid User", "Password123!");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserCommand.Email));
    }

    // ─── Name validation ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyName_ReturnsNameError(string name)
    {
        // Arrange
        var command = new CreateUserCommand("valid@umbral.io", name, "Password123!");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserCommand.Name));
    }

    [Fact]
    public void Validate_WithNameExceeding200Chars_ReturnsNameError()
    {
        // Arrange
        var longName = new string('A', 201);
        var command = new CreateUserCommand("valid@umbral.io", longName, "Password123!");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserCommand.Name));
    }

    [Fact]
    public void Validate_WithNameAt200Chars_ReturnsNoNameError()
    {
        // Arrange
        var maxName = new string('A', 200);
        var command = new CreateUserCommand("valid@umbral.io", maxName, "Password123!");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateUserCommand.Name));
    }

    // ─── Password validation ──────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyPassword_ReturnsPasswordError(string password)
    {
        // Arrange
        var command = new CreateUserCommand("valid@umbral.io", "Valid User", password);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserCommand.Password));
    }

    [Fact]
    public void Validate_WithPasswordShorterThan8Chars_ReturnsPasswordError()
    {
        // Arrange
        var command = new CreateUserCommand("valid@umbral.io", "Valid User", "Short1");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserCommand.Password));
    }

    [Fact]
    public void Validate_WithPasswordOf8Chars_ReturnsNoPasswordError()
    {
        // Arrange
        var command = new CreateUserCommand("valid@umbral.io", "Valid User", "Pass123!");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateUserCommand.Password));
    }

    // ─── Multiple errors ──────────────────────────────────────────────────────

    [Fact]
    public void Validate_WithAllFieldsInvalid_ReturnsMultipleErrors()
    {
        // Arrange
        var command = new CreateUserCommand("", "", "");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    // ─── Role validation ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("admin")]
    [InlineData("operator")]
    [InlineData("player")]
    public void Validate_WithValidRole_ReturnsNoRoleError(string role)
    {
        // Arrange
        var command = new CreateUserCommand("valid@umbral.io", "Valid User", "Password123!", role);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateUserCommand.Role));
    }

    [Theory]
    [InlineData("superadmin")]
    [InlineData("moderator")]
    [InlineData("")]
    [InlineData("ADMIN")]
    public void Validate_WithInvalidRole_ReturnsRoleError(string role)
    {
        // Arrange
        var command = new CreateUserCommand("valid@umbral.io", "Valid User", "Password123!", role);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserCommand.Role));
    }

    [Fact]
    public void Validate_WithDefaultRole_ReturnsNoRoleError()
    {
        // Arrange — default role is "player"
        var command = new CreateUserCommand("valid@umbral.io", "Valid User", "Password123!");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateUserCommand.Role));
    }
}
