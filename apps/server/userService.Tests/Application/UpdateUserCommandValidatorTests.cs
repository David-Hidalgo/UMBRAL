using System;
using FluentAssertions;
using UserService.Application.Command.UpdateUser;
using Xunit;

namespace UserService.Tests.Application;

public class UpdateUserCommandValidatorTests
{
    private readonly UpdateUserCommandValidator _sut = new();

    private static UpdateUserCommand ValidCommand(string role = "player")
        => new(Guid.NewGuid(), "Valid Name", role);

    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        var result = _sut.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyId_ReturnsIdError()
    {
        var command = new UpdateUserCommand(Guid.Empty, "Valid Name", "player");

        var result = _sut.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateUserCommand.Id));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyName_ReturnsNameError(string name)
    {
        var command = new UpdateUserCommand(Guid.NewGuid(), name, "player");

        var result = _sut.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateUserCommand.Name));
    }

    [Fact]
    public void Validate_WithNameExceeding200Chars_ReturnsNameError()
    {
        var command = new UpdateUserCommand(Guid.NewGuid(), new string('A', 201), "player");

        var result = _sut.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateUserCommand.Name));
    }

    [Fact]
    public void Validate_WithNameAt200Chars_ReturnsNoNameError()
    {
        var command = new UpdateUserCommand(Guid.NewGuid(), new string('A', 200), "player");

        var result = _sut.Validate(command);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateUserCommand.Name));
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("operator")]
    [InlineData("player")]
    public void Validate_WithValidRole_ReturnsNoRoleError(string role)
    {
        var result = _sut.Validate(ValidCommand(role));

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateUserCommand.Role));
    }

    [Theory]
    [InlineData("superadmin")]
    [InlineData("ADMIN")]
    [InlineData("")]
    public void Validate_WithInvalidRole_ReturnsRoleError(string role)
    {
        var command = new UpdateUserCommand(Guid.NewGuid(), "Valid Name", role);

        var result = _sut.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateUserCommand.Role));
    }
}
