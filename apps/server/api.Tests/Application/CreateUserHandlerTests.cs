using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using api.Application.Commands;
using api.Application.DTOs;
using api.Application.Handlers;
using api.Application.Interfaces;
using Xunit;

namespace api.Tests.Application;

public class CreateUserHandlerTests
{
    private readonly Mock<IUserServiceClient> _clientMock;
    private readonly Mock<ILogger<CreateUserHandler>> _loggerMock;
    private readonly CreateUserHandler _sut;

    public CreateUserHandlerTests()
    {
        _clientMock = new Mock<IUserServiceClient>(MockBehavior.Strict);
        // ILogger usa MockBehavior.Loose: los métodos de extensión Log<TState>
        // son difíciles de configurar con Strict sin comprometer la legibilidad.
        _loggerMock = new Mock<ILogger<CreateUserHandler>>(MockBehavior.Loose);
        _sut = new CreateUserHandler(_clientMock.Object, _loggerMock.Object);
    }

    private static CreateUserRequest ValidRequest() =>
        new("admin@umbral.io", "Admin UMBRAL", "Password123!");

    private static CreateUserCommand ValidCommand() =>
        new(ValidRequest());

    // ─── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsGuidFromClient()
    {
        // Arrange
        var expected = Guid.NewGuid();

        _clientMock
            .Setup(c => c.CreateUserAsync(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.Handle(ValidCommand(), CancellationToken.None);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CallsClientExactlyOnce()
    {
        // Arrange
        _clientMock
            .Setup(c => c.CreateUserAsync(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        await _sut.Handle(ValidCommand(), CancellationToken.None);

        // Assert
        _clientMock.Verify(
            c => c.CreateUserAsync(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_PassesRequestDataToClient()
    {
        // Arrange
        var command = ValidCommand();
        CreateUserRequest? captured = null;

        _clientMock
            .Setup(c => c.CreateUserAsync(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateUserRequest, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(Guid.NewGuid());

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        captured!.Email.Should().Be(command.Request.Email);
        captured.Name.Should().Be(command.Request.Name);
        captured.Password.Should().Be(command.Request.Password);
    }

    // ─── Sad path ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenClientThrows_PropagatesException()
    {
        // Arrange
        _clientMock
            .Setup(c => c.CreateUserAsync(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("UserService unavailable"));

        // Act
        var act = async () => await _sut.Handle(ValidCommand(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*unavailable*");
    }
}
