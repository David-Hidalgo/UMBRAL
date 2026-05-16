using System;
using FluentAssertions;
using UserService.Domain.Entities;
using Xunit;

namespace UserService.Tests.Domain;

public class UserTests
{
    [Fact]
    public void Create_WithValidParameters_ReturnsUserWithCorrectProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        const string keycloakUserId = "kc-abc-123";
        const string email = "admin@umbral.io";
        const string name = "Admin UMBRAL";

        // Act
        var user = User.Create(id, keycloakUserId, email, name);

        // Assert
        user.Id.Should().Be(id);
        user.KeycloakUserId.Should().Be(keycloakUserId);
        user.Email.Value.Should().Be(email);
        user.Name.Value.Should().Be(name);
    }

    [Fact]
    public void Create_WithMixedCaseEmail_NormalizesToLowercase()
    {
        // Act
        var user = User.Create(Guid.NewGuid(), "kc-123", "ADMIN@UMBRAL.IO", "Admin");

        // Assert
        user.Email.Value.Should().Be("admin@umbral.io");
    }

    [Fact]
    public void Create_WithEmptyGuid_ThrowsArgumentException()
    {
        // Act
        var act = () => User.Create(Guid.Empty, "kc-123", "admin@umbral.io", "Admin");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*User ID*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankKeycloakUserId_ThrowsArgumentException(string keycloakUserId)
    {
        // Act
        var act = () => User.Create(Guid.NewGuid(), keycloakUserId, "admin@umbral.io", "Admin");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Keycloak user ID*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankEmail_ThrowsArgumentException(string email)
    {
        // Act
        var act = () => User.Create(Guid.NewGuid(), "kc-123", email, "Admin");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithInvalidEmailFormat_ThrowsArgumentException()
    {
        // Act
        var act = () => User.Create(Guid.NewGuid(), "kc-123", "not-an-email", "Admin");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*format*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_ThrowsArgumentException(string name)
    {
        // Act
        var act = () => User.Create(Guid.NewGuid(), "kc-123", "admin@umbral.io", name);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNameExceeding200Chars_ThrowsArgumentException()
    {
        // Arrange
        var longName = new string('A', 201);

        // Act
        var act = () => User.Create(Guid.NewGuid(), "kc-123", "admin@umbral.io", longName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*200 characters*");
    }
}
