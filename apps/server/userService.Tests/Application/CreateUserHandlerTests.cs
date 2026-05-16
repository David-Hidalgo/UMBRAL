using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using UserService.Application.Commands;
using UserService.Application.Handlers;
using UserService.Application.IntegrationEvents;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.RepositoryInterfaces;
using Xunit;

namespace UserService.Tests.Application;

public class CreateUserHandlerTests
{
    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly Mock<IEventBus> _eventBusMock;
    private readonly Mock<IIdentityProviderService> _identityMock;
    private readonly CreateUserHandler _sut;

    public CreateUserHandlerTests()
    {
        _repositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);
        _eventBusMock   = new Mock<IEventBus>(MockBehavior.Strict);
        _identityMock   = new Mock<IIdentityProviderService>(MockBehavior.Strict);
        _sut = new CreateUserHandler(_repositoryMock.Object, _eventBusMock.Object, _identityMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static CreateUserCommand ValidCommand() =>
        new("admin@umbral.io", "Admin UMBRAL", "Password123!");

    private void SetupHappyPath(CreateUserCommand cmd, string externalId = "kc-abc-123")
    {
        _identityMock
            .Setup(s => s.CreateUserAsync(cmd.Email, cmd.Name, cmd.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(externalId);

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

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
    public async Task Handle_WithValidCommand_ReturnedIdMatchesPersistedUser()
    {
        // Arrange
        var cmd = ValidCommand();
        User? captured = null;

        _identityMock
            .Setup(s => s.CreateUserAsync(cmd.Email, cmd.Name, cmd.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync("kc-abc-123");

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => captured = u)
            .Returns(Task.CompletedTask);

        _eventBusMock
            .Setup(e => e.PublishAsync(It.IsAny<UserCreated>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().Be(captured!.Id);
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
            s => s.CreateUserAsync(cmd.Email, cmd.Name, cmd.Password, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_PersistsUserWithCorrectProperties()
    {
        // Arrange
        const string externalId = "kc-external-999";
        var cmd = ValidCommand();
        User? captured = null;

        _identityMock
            .Setup(s => s.CreateUserAsync(cmd.Email, cmd.Name, cmd.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(externalId);

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => captured = u)
            .Returns(Task.CompletedTask);

        _eventBusMock
            .Setup(e => e.PublishAsync(It.IsAny<UserCreated>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        captured!.Id.Should().NotBe(Guid.Empty);
        captured.KeycloakUserId.Should().Be(externalId);
        captured.Email.Value.Should().Be(cmd.Email.ToLowerInvariant());
        captured.Name.Value.Should().Be(cmd.Name);
    }

    [Fact]
    public async Task Handle_WithValidCommand_PublishesUserCreatedEventWithCorrectData()
    {
        // Arrange
        const string externalId = "kc-external-999";
        var cmd = ValidCommand();
        UserCreated? capturedEvent = null;

        _identityMock
            .Setup(s => s.CreateUserAsync(cmd.Email, cmd.Name, cmd.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(externalId);

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

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
        capturedEvent.UserId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CallsRepositoryAndEventBusExactlyOnce()
    {
        // Arrange
        var cmd = ValidCommand();
        SetupHappyPath(cmd);

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _eventBusMock.Verify(e => e.PublishAsync(It.IsAny<UserCreated>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Sad paths ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenIdentityProviderThrows_PropagatesExceptionWithoutCallingRepository()
    {
        // Arrange
        var cmd = ValidCommand();

        _identityMock
            .Setup(s => s.CreateUserAsync(cmd.Email, cmd.Name, cmd.Password, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Keycloak unavailable"));

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Keycloak*");

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _eventBusMock.Verify(e => e.PublishAsync(It.IsAny<UserCreated>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrows_PropagatesExceptionWithoutPublishingEvent()
    {
        // Arrange
        var cmd = ValidCommand();

        _identityMock
            .Setup(s => s.CreateUserAsync(cmd.Email, cmd.Name, cmd.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync("kc-123");

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database unavailable"));

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Database*");

        _eventBusMock.Verify(e => e.PublishAsync(It.IsAny<UserCreated>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
