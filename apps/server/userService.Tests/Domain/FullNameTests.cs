using System;
using FluentAssertions;
using UserService.Domain.ValueObjects;
using Xunit;

namespace UserService.Tests.Domain;

public class FullNameTests
{
    [Fact]
    public void Create_WithValidName_ReturnsTrimmedValue()
    {
        // Act
        var name = FullName.Create("  John Doe  ");

        // Assert
        name.Value.Should().Be("John Doe");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespace_ThrowsArgumentException(string input)
    {
        // Act
        var act = () => FullName.Create(input);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public void Create_WithNameExactlyAt200Chars_Succeeds()
    {
        // Arrange
        var maxName = new string('A', 200);

        // Act
        var name = FullName.Create(maxName);

        // Assert
        name.Value.Should().HaveLength(200);
    }

    [Fact]
    public void Create_WithNameOf201Chars_ThrowsArgumentException()
    {
        // Arrange
        var longName = new string('A', 201);

        // Act
        var act = () => FullName.Create(longName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*200 characters*");
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        // Arrange
        var name = FullName.Create("John Doe");

        // Assert
        name.ToString().Should().Be("John Doe");
    }

    [Fact]
    public void TwoInstancesWithSameValue_AreStructurallyEqual()
    {
        // Arrange
        var a = FullName.Create("John Doe");
        var b = FullName.Create("John Doe");

        // Assert
        a.Should().Be(b);
    }

    [Fact]
    public void TwoInstancesWithDifferentValues_AreNotEqual()
    {
        // Arrange
        var a = FullName.Create("Alice");
        var b = FullName.Create("Bob");

        // Assert
        a.Should().NotBe(b);
    }
}
