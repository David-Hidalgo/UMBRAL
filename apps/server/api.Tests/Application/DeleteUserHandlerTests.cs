using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using api.Application.Commands;
using api.Application.Handlers;
using api.Application.Interfaces;
using Xunit;

namespace api.Tests.Application;

public class DeleteUserHandlerTests
{
    private readonly Mock<IUserServiceClient> _clientMock;
    private readonly Mock<ILogger<DeleteUserHandler>> _loggerMock;
    private readonly DeleteUserHandler _sut;

    public DeleteUserHandlerTests()
    {
        _clientMock = new Mock<IUserServiceClient>(MockBehavior.Strict);
        _loggerMock = new Mock<ILogger<DeleteUserHandler>>(MockBehavior.Loose);
        _sut = new DeleteUserHandler(_clientMock.Object, _loggerMock.Object);
    }

    private static DeleteUserCommand ValidCommand(string kcId = "kc-user-123") => new(kcId);

    // ─── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidCommand_CallsClientExactlyOnce()
    {
        // Arrange
        _clientMock
            .Setup(c => c.DeleteUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(ValidCommand(), CancellationToken.None);

        // Assert
        _clientMock.Verify(
            c => c.DeleteUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_PassesCorrectKeycloakIdToClient()
    {
        // Arrange
        const string keycloakId = "kc-specific-456";
        string? captured = null;

        _clientMock
            .Setup(c => c.DeleteUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((id, _) => captured = id)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(ValidCommand(keycloakId), CancellationToken.None);

        // Assert
        captured.Should().Be(keycloakId);
    }

    // ─── Sad path ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenClientThrows_PropagatesException()
    {
        // Arrange
        _clientMock
            .Setup(c => c.DeleteUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("UserService unavailable"));

        // Act
        var act = async () => await _sut.Handle(ValidCommand(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*unavailable*");
    }
}
