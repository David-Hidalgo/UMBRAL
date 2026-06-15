using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using UserService.Application.Commands;
using UserService.Application.Handlers;
using UserService.Application.IntegrationEvents;
using UserService.Application.Interfaces;
using Xunit;

namespace UserService.Tests.Application;

public class CreateUserHandlerTests
{
    private readonly Mock<IEventBus> _eventBusMock;
    private readonly Mock<IIdentityProviderService> _identityMock;
    private readonly CreateUserHandler _sut;

    public CreateUserHandlerTests()
    {
        _eventBusMock = new Mock<IEventBus>(MockBehavior.Strict);
        _identityMock   = new Mock<IIdentityProviderService>(MockBehavior.Strict);
        _sut = new CreateUserHandler(_eventBusMock.Object, _identityMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static CreateUserCommand ValidCommand(string role = "operator") =>
        new("admin@umbral.io", "Admin UMBRAL", role);

    private void SetupHappyPath(CreateUserCommand cmd, string externalId = "kc-abc-123")
    {
        _identityMock
            .Setup(s => s.CreateUserAsync(cmd.Email, cmd.Name, cmd.Role, It.IsAny<CancellationToken>()))
            .ReturnsAsync(externalId);

        _eventBusMock
            .Setup(e => e.PublishAsync(It.IsAny<UserCreated>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    // ─── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsNonEmptyGuid()
    {
        // Arrange
        var cmd = ValidCommand();
        SetupHappyPath(cmd);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnedIdMatchesEventUserId()
    {
        // Arrange
        var cmd = ValidCommand();
        UserCreated? capturedEvent = null;

        _identityMock
            .Setup(s => s.CreateUserAsync(cmd.Email, cmd.Name, cmd.Role, It.IsAny<CancellationToken>()))
            .ReturnsAsync("kc-abc-123");

        _eventBusMock
            .Setup(e => e.PublishAsync(It.IsAny<UserCreated>(), It.IsAny<CancellationToken>()))
            .Callback<UserCreated, CancellationToken>((evt, _) => capturedEvent = evt)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().Be(capturedEvent!.UserId);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CallsIdentityProviderExactlyOnce()
    {
        // Arrange
        var cmd = ValidCommand();
        SetupHappyPath(cmd);

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        _identityMock.Verify(
            s => s.CreateUserAsync(cmd.Email, cmd.Name, cmd.Role, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_PublishesEventWithCorrectProperties()
    {
        // Arrange
        const string externalId = "kc-external-999";
        var cmd = ValidCommand();
        UserCreated? capturedEvent = null;

        _identityMock
            .Setup(s => s.CreateUserAsync(cmd.Email, cmd.Name, cmd.Role, It.IsAny<CancellationToken>()))
            .ReturnsAsync(externalId);

        _eventBusMock
            .Setup(e => e.PublishAsync(It.IsAny<UserCreated>(), It.IsAny<CancellationToken>()))
            .Callback<UserCreated, CancellationToken>((evt, _) => capturedEvent = evt)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.UserId.Should().NotBe(Guid.Empty);
        capturedEvent.KeycloakUserId.Should().Be(externalId);
        capturedEvent.Email.Should().Be(cmd.Email.ToLowerInvariant());
        capturedEvent.Role.Should().Be(cmd.Role);
    }

    [Fact]
    public async Task Handle_WithValidCommand_PublishesUserCreatedEventWithCorrectData()
    {
        // Arrange
        const string externalId = "kc-external-999";
        var cmd = ValidCommand();
        UserCreated? capturedEvent = null;

        _identityMock
            .Setup(s => s.CreateUserAsync(cmd.Email, cmd.Name, cmd.Role, It.IsAny<CancellationToken>()))
            .ReturnsAsync(externalId);

        _eventBusMock
            .Setup(e => e.PublishAsync(It.IsAny<UserCreated>(), It.IsAny<CancellationToken>()))
            .Callback<UserCreated, CancellationToken>((evt, _) => capturedEvent = evt)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.KeycloakUserId.Should().Be(externalId);
        capturedEvent.Email.Should().Be(cmd.Email.ToLowerInvariant());
        capturedEvent.Role.Should().Be(cmd.Role);
        capturedEvent.UserId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CallsEventBusExactlyOnce()
    {
        // Arrange
        var cmd = ValidCommand();
        SetupHappyPath(cmd);

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        _eventBusMock.Verify(e => e.PublishAsync(It.IsAny<UserCreated>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Sad paths ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenIdentityProviderThrows_PropagatesExceptionWithoutPublishingEvent()
    {
        // Arrange
        var cmd = ValidCommand();

        _identityMock
            .Setup(s => s.CreateUserAsync(cmd.Email, cmd.Name, cmd.Role, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Keycloak unavailable"));

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Keycloak*");

        _eventBusMock.Verify(e => e.PublishAsync(It.IsAny<UserCreated>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("operator")]
    public async Task Handle_WithExplicitRole_PublishesEventWithThatRole(string role)
    {
        // Arrange
        var cmd = ValidCommand(role);
        UserCreated? capturedEvent = null;

        _identityMock
            .Setup(s => s.CreateUserAsync(cmd.Email, cmd.Name, cmd.Role, It.IsAny<CancellationToken>()))
            .ReturnsAsync("kc-abc-123");

        _eventBusMock
            .Setup(e => e.PublishAsync(It.IsAny<UserCreated>(), It.IsAny<CancellationToken>()))
            .Callback<UserCreated, CancellationToken>((evt, _) => capturedEvent = evt)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.Role.Should().Be(role);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("operator")]
    public async Task Handle_WithRole_PublishesEventWithThatRole(string role)
    {
        // Arrange
        var cmd = ValidCommand(role);
        UserCreated? capturedEvent = null;

        _identityMock
            .Setup(s => s.CreateUserAsync(cmd.Email, cmd.Name, cmd.Role, It.IsAny<CancellationToken>()))
            .ReturnsAsync("kc-abc-123");

        _eventBusMock
            .Setup(e => e.PublishAsync(It.IsAny<UserCreated>(), It.IsAny<CancellationToken>()))
            .Callback<UserCreated, CancellationToken>((evt, _) => capturedEvent = evt)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.Role.Should().Be(role);
    }
}
