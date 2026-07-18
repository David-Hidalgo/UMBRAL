using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using UserService.Application.Command.DeleteUser;
using UserService.Application.DTOs;
using UserService.Application.IntegrationEvents;
using UserService.Application.Abstractions;
using Xunit;

namespace UserService.Tests.Application;

public class DeleteUserHandlerTests
{
    private readonly Mock<IIdentityProviderService> _identityMock;
    private readonly Mock<IEventBus> _eventBusMock;
    private readonly DeleteUserHandler _sut;

    public DeleteUserHandlerTests()
    {
        _identityMock = new Mock<IIdentityProviderService>(MockBehavior.Strict);
        _eventBusMock = new Mock<IEventBus>(MockBehavior.Strict);
        _sut = new DeleteUserHandler(_identityMock.Object, _eventBusMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private const string KcId = "kc-user-123";

    private static UserDto MakeActiveUserDto(string kcId = KcId)
        => new(Guid.NewGuid(), kcId, "test@umbral.io", "Test User", "player", DateTime.UtcNow, IsEnabled: true);

    private static UserDto MakeInactiveUserDto(string kcId = KcId)
        => new(Guid.NewGuid(), kcId, "test@umbral.io", "Test User", "player", DateTime.UtcNow, IsEnabled: false);

    private static DeleteUserCommand ValidCommand(string kcId = KcId)
        => new(kcId);

    private void SetupHappyPath(UserDto user)
    {
        _identityMock
            .Setup(i => i.GetUserByIdAsync(user.KeycloakUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _identityMock
            .Setup(i => i.SetUserEnabledAsync(user.KeycloakUserId, false, It.IsAny<CancellationToken>()))
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
        var user = MakeActiveUserDto();
        SetupHappyPath(user);

        // Act
        await _sut.Handle(ValidCommand(), CancellationToken.None);

        // Assert
        _identityMock.Verify(
            i => i.SetUserEnabledAsync(KcId, false, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_PublishesUserDeactivatedEvent()
    {
        // Arrange
        var user = MakeActiveUserDto();
        UserDeactivated? capturedEvent = null;

        _identityMock
            .Setup(i => i.GetUserByIdAsync(KcId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _identityMock
            .Setup(i => i.SetUserEnabledAsync(KcId, false, It.IsAny<CancellationToken>()))
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
    }

    // ─── Sad paths ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _identityMock
            .Setup(i => i.GetUserByIdAsync(KcId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserDto?)null);

        // Act
        var act = async () => await _sut.Handle(ValidCommand(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{KcId}*");

        _identityMock.Verify(
            i => i.SetUserEnabledAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _eventBusMock.Verify(
            e => e.PublishAsync(It.IsAny<UserDeactivated>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyInactive_ThrowsInvalidOperationException()
    {
        // Arrange
        var user = MakeInactiveUserDto();

        _identityMock
            .Setup(i => i.GetUserByIdAsync(KcId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var act = async () => await _sut.Handle(ValidCommand(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already inactive*");

        _identityMock.Verify(
            i => i.SetUserEnabledAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenKeycloakFails_ThrowsAndDoesNotPublishEvent()
    {
        // Arrange
        var user = MakeActiveUserDto();

        _identityMock
            .Setup(i => i.GetUserByIdAsync(KcId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _identityMock
            .Setup(i => i.SetUserEnabledAsync(KcId, false, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Keycloak unavailable"));

        // Act
        var act = async () => await _sut.Handle(ValidCommand(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Keycloak*");

        _eventBusMock.Verify(
            e => e.PublishAsync(It.IsAny<UserDeactivated>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenGetUserByIdThrows_PropagatesException()
    {
        // Arrange
        _identityMock
            .Setup(i => i.GetUserByIdAsync(KcId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Keycloak unavailable"));

        // Act
        var act = async () => await _sut.Handle(ValidCommand(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Keycloak*");

        _identityMock.Verify(
            i => i.SetUserEnabledAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSetUserEnabledThrows_DoesNotPublishEvent()
    {
        // Arrange
        var user = MakeActiveUserDto();

        _identityMock
            .Setup(i => i.GetUserByIdAsync(KcId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _identityMock
            .Setup(i => i.SetUserEnabledAsync(KcId, false, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Keycloak unavailable"));

        // Act
        var act = async () => await _sut.Handle(ValidCommand(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _eventBusMock.Verify(
            e => e.PublishAsync(It.IsAny<UserDeactivated>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
