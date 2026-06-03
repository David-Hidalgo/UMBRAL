using System;
using FluentAssertions;
using UserService.Domain.Entities;
using Xunit;

namespace UserService.Tests.Domain;

public class UserUpdateProfileTests
{
    private static User MakeUser(string role = "player")
        => User.Create(Guid.NewGuid(), "kc-abc-123", "user@umbral.io", "Original Name", role);

    [Fact]
    public void UpdateProfile_WithValidData_UpdatesNameAndRole()
    {
        // Arrange
        var user = MakeUser();

        // Act
        user.UpdateProfile("New Name", "operator");

        // Assert
        user.Name.Value.Should().Be("New Name");
        user.Role.Should().Be("operator");
    }

    [Fact]
    public void UpdateProfile_TrimsName()
    {
        // Arrange
        var user = MakeUser();

        // Act
        user.UpdateProfile("  Padded Name  ", "player");

        // Assert
        user.Name.Value.Should().Be("Padded Name");
    }

    [Fact]
    public void UpdateProfile_DoesNotChangeEmailOrKeycloakId()
    {
        // Arrange
        var user = MakeUser();
        var originalEmail = user.Email.Value;
        var originalKeycloakId = user.KeycloakUserId;

        // Act
        user.UpdateProfile("Another Name", "admin");

        // Assert
        user.Email.Value.Should().Be(originalEmail);
        user.KeycloakUserId.Should().Be(originalKeycloakId);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("operator")]
    [InlineData("player")]
    public void UpdateProfile_WithValidRole_SetsRole(string role)
    {
        // Arrange
        var user = MakeUser();

        // Act
        user.UpdateProfile("Name", role);

        // Assert
        user.Role.Should().Be(role);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateProfile_WithBlankName_ThrowsArgumentException(string name)
    {
        // Arrange
        var user = MakeUser();

        // Act
        var act = () => user.UpdateProfile(name, "player");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateProfile_WithNameExceeding200Chars_ThrowsArgumentException()
    {
        // Arrange
        var user = MakeUser();
        var longName = new string('A', 201);

        // Act
        var act = () => user.UpdateProfile(longName, "player");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*200 characters*");
    }

    [Theory]
    [InlineData("superadmin")]
    [InlineData("ADMIN")]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateProfile_WithInvalidRole_ThrowsArgumentException(string role)
    {
        // Arrange
        var user = MakeUser();

        // Act
        var act = () => user.UpdateProfile("Name", role);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Role*");
    }

    [Fact]
    public void UpdateProfile_WhenRoleInvalid_DoesNotMutateState()
    {
        // Arrange
        var user = MakeUser("player");

        // Act
        var act = () => user.UpdateProfile("Valid Name", "invalid-role");

        // Assert — la validacion ocurre antes de cualquier mutacion (invariante atomica).
        act.Should().Throw<ArgumentException>();
        user.Role.Should().Be("player");
        user.Name.Value.Should().Be("Original Name");
    }

    [Fact]
    public void Create_WithInvalidRole_ThrowsArgumentException()
    {
        // Act
        var act = () => User.Create(Guid.NewGuid(), "kc-123", "user@umbral.io", "Name", "invalid");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Role*");
    }
}
