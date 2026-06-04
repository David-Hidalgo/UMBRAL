using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using UserService.Application.Commands;
using UserService.Application.Handlers;
using UserService.Application.IntegrationEvents;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.RepositoryInterfaces;
using Xunit;

namespace UserService.Tests.Application;

public class DeleteUserHandlerTests
{
    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly Mock<IIdentityProviderService> _identityMock;
    private readonly Mock<IEventBus> _eventBusMock;
    private readonly Mock<ILogger<DeleteUserHandler>> _loggerMock;
    private readonly DeleteUserHandler _sut;

    public DeleteUserHandlerTests()
    {
        _repositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);
        _identityMock   = new Mock<IIdentityProviderService>(MockBehavior.Strict);
        _eventBusMock   = new Mock<IEventBus>(MockBehavior.Strict);
        _loggerMock     = new Mock<ILogger<DeleteUserHandler>>(MockBehavior.Loose);
        _sut = new DeleteUserHandler(
            _repositoryMock.Object,
            _identityMock.Object,
            _eventBusMock.Object,
            _loggerMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private const string KcId = "kc-user-123";

    private static User MakeActiveUser(string kcId = KcId)
        => User.Create(Guid.NewGuid(), kcId, "test@umbral.io", "Test User");

    private static DeleteUserCommand ValidCommand(string kcId = KcId)
        => new(kcId);

    private void SetupHappyPath(User user)
    {
        _repositoryMock
            .Setup(r => r.GetByKeycloakIdAsync(user.KeycloakUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _identityMock
            .Setup(i => i.SetUserEnabledAsync(user.KeycloakUserId, false, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _eventBusMock
            .Setup(e => e.PublishAsync(It.IsAny<UserDeactivated>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    // ─── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidCommand_CallsSetUserEnabledWithFalse()
    {
        // Arrange
        var user = MakeActiveUser();
        SetupHappyPath(user);

        // Act
        await _sut.Handle(ValidCommand(), CancellationToken.None);

        // Assert
        _identityMock.Verify(
            i => i.SetUserEnabledAsync(KcId, false, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CallsUserDeactivate()
    {
        // Arrange
        var user = MakeActiveUser();
        User? captured = null;

        _repositoryMock
            .Setup(r => r.GetByKeycloakIdAsync(KcId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _identityMock
            .Setup(i => i.SetUserEnabledAsync(KcId, false, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => captured = u)
            .Returns(Task.CompletedTask);

        _eventBusMock
            .Setup(e => e.PublishAsync(It.IsAny<UserDeactivated>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(ValidCommand(), CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        captured!.IsActive.Should().BeFalse();
        captured.DeactivatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WithValidCommand_CallsUpdateAsync()
    {
        // Arrange
        var user = MakeActiveUser();
        SetupHappyPath(user);

        // Act
        await _sut.Handle(ValidCommand(), CancellationToken.None);

        // Assert
        _repositoryMock.Verify(
            r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_PublishesUserDeactivatedEvent()
    {
        // Arrange
        var user = MakeActiveUser();
        UserDeactivated? capturedEvent = null;

        _repositoryMock
            .Setup(r => r.GetByKeycloakIdAsync(KcId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _identityMock
            .Setup(i => i.SetUserEnabledAsync(KcId, false, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _eventBusMock
            .Setup(e => e.PublishAsync(It.IsAny<UserDeactivated>(), It.IsAny<CancellationToken>()))
            .Callback<UserDeactivated, CancellationToken>((evt, _) => capturedEvent = evt)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(ValidCommand(), CancellationToken.None);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.KeycloakUserId.Should().Be(KcId);
        capturedEvent.UserId.Should().Be(user.Id);
    }

    // ─── Sad paths ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange — user not in local repo, and Keycloak confirms it doesn't exist either
        _repositoryMock
            .Setup(r => r.GetByKeycloakIdAsync(KcId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _identityMock
            .Setup(i => i.SetUserEnabledAsync(KcId, false, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"User '{KcId}' was not found in Keycloak."));

        // Act
        var act = async () => await _sut.Handle(ValidCommand(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{KcId}*");
    }

    [Fact]
    public async Task Handle_WhenUserNotInLocalRepo_StillDisablesInKeycloak()
    {
        // Arrange — simulates server restart: user exists in Keycloak but not in InMemory
        _repositoryMock
            .Setup(r => r.GetByKeycloakIdAsync(KcId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _identityMock
            .Setup(i => i.SetUserEnabledAsync(KcId, false, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(ValidCommand(), CancellationToken.None);

        // Assert — Keycloak was disabled; local repo and event bus were NOT touched
        _identityMock.Verify(
            i => i.SetUserEnabledAsync(KcId, false, It.IsAny<CancellationToken>()),
            Times.Once);
        _repositoryMock.Verify(
            r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyInactive_ThrowsInvalidOperationException()
    {
        // Arrange
        var user = MakeActiveUser();
        user.Deactivate();

        _repositoryMock
            .Setup(r => r.GetByKeycloakIdAsync(KcId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var act = async () => await _sut.Handle(ValidCommand(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already inactive*");
    }

    [Fact]
    public async Task Handle_WhenKeycloakFails_ThrowsAndDoesNotCallRepository()
    {
        // Arrange
        var user = MakeActiveUser();

        _repositoryMock
            .Setup(r => r.GetByKeycloakIdAsync(KcId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _identityMock
            .Setup(i => i.SetUserEnabledAsync(KcId, false, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Keycloak unavailable"));

        // Act
        var act = async () => await _sut.Handle(ValidCommand(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Keycloak*");

        // With MockBehavior.Strict, UpdateAsync is never set up — any call would throw
        _repositoryMock.Verify(
            r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUpdateAsyncFails_AttemptsRollbackOnKeycloak()
    {
        // Arrange
        var user = MakeActiveUser();

        _repositoryMock
            .Setup(r => r.GetByKeycloakIdAsync(KcId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _identityMock
            .Setup(i => i.SetUserEnabledAsync(KcId, false, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB failure"));

        _identityMock
            .Setup(i => i.SetUserEnabledAsync(KcId, true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var act = async () => await _sut.Handle(ValidCommand(), CancellationToken.None);

        // Assert: original exception propagates
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*DB*");

        // Rollback was called
        _identityMock.Verify(
            i => i.SetUserEnabledAsync(KcId, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUpdateAsyncAndRollbackBothFail_PropagatesOriginalException()
    {
        // Arrange
        var user = MakeActiveUser();

        _repositoryMock
            .Setup(r => r.GetByKeycloakIdAsync(KcId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _identityMock
            .Setup(i => i.SetUserEnabledAsync(KcId, false, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB failure"));

        _identityMock
            .Setup(i => i.SetUserEnabledAsync(KcId, true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Keycloak rollback failed"));

        // Act
        var act = async () => await _sut.Handle(ValidCommand(), CancellationToken.None);

        // Assert: the ORIGINAL DB exception propagates (rollback failure is swallowed + logged)
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*DB*");
    }
}
