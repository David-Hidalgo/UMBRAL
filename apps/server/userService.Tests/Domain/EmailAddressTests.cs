using System;
using FluentAssertions;
using UserService.Domain.ValueObjects;
using Xunit;

namespace UserService.Tests.Domain;

public class EmailAddressTests
{
    [Theory]
    [InlineData("user@example.com", "user@example.com")]
    [InlineData("USER@EXAMPLE.COM", "user@example.com")]
    [InlineData("  user@example.com  ", "user@example.com")]
    [InlineData("Admin@UMBRAL.IO", "admin@umbral.io")]
    public void Create_WithValidEmail_ReturnsNormalizedValue(string input, string expected)
    {
        // Act
        var email = EmailAddress.Create(input);

        // Assert
        email.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespace_ThrowsArgumentException(string input)
    {
        // Act
        var act = () => EmailAddress.Create(input);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }

    [Theory]
    [InlineData("noatsymbol")]
    [InlineData("missing-dot@domain")]
    [InlineData("@example.com")]
    [InlineData("\t@example.com")]
    [InlineData("  @example.com  ")]
    [InlineData("\tmissing-dot@domain")]
    public void Create_WithInvalidFormat_ThrowsArgumentException(string input)
    {
        // Act
        var act = () => EmailAddress.Create(input);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*format*");
    }

    [Fact]
    public void ToString_ReturnsNormalizedValue()
    {
        // Arrange
        var email = EmailAddress.Create("USER@EXAMPLE.COM");

        // Assert
        email.ToString().Should().Be("user@example.com");
    }

    [Fact]
    public void TwoInstancesWithSameValue_AreStructurallyEqual()
    {
        // Arrange
        var a = EmailAddress.Create("user@example.com");
        var b = EmailAddress.Create("USER@EXAMPLE.COM");

        // Assert
        a.Should().Be(b);
    }

    [Fact]
    public void TwoInstancesWithDifferentValues_AreNotEqual()
    {
        // Arrange
        var a = EmailAddress.Create("a@example.com");
        var b = EmailAddress.Create("b@example.com");

        // Assert
        a.Should().NotBe(b);
    }
}
