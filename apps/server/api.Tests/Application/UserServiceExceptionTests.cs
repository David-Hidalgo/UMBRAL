using FluentAssertions;
using api.Application.Exceptions;
using Xunit;

namespace api.Tests.Application;

public class UserServiceExceptionTests
{
    [Fact]
    public void DefaultConstructor_SetsDefaultMessageAndStatusCode502()
    {
        // Act
        var ex = new UserServiceException();

        // Assert
        ex.StatusCode.Should().Be(502);
        ex.Message.Should().Be("Upstream user service error.");
    }

    [Fact]
    public void ContentConstructor_ContentPropertyIsReadable()
    {
        // Arrange
        const string content = "{\"error\":\"User not found\"}";

        // Act
        var ex = new UserServiceException(404, content, "UserService returned 404: not found");

        // Assert
        ex.Content.Should().Be(content);
        ex.StatusCode.Should().Be(404);
    }
}
