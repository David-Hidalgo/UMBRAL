using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using UserService.Application.Commands;
using UserService.Application.DTOs;
using UserService.Application.Handlers;
using UserService.Application.IntegrationEvents;
using UserService.Application.Interfaces;
using Xunit;

namespace UserService.Tests.Application;

public class UpdateUserHandlerTests
{
    private readonly Mock<IEventBus> _eventBusMock;
    private readonly Mock<IIdentityProviderService> _identityMock;
    private readonly UpdateUserHandler _sut;

    private readonly Guid _id = Guid.NewGuid();

    public UpdateUserHandlerTests()
    {
        _eventBusMock = new Mock<IEventBus>(MockBehavior.Strict);
        _identityMock = new Mock<IIdentityProviderService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<UpdateUserHandler>>(MockBehavior.Loose);
        _sut = new UpdateUserHandler(_eventBusMock.Object, _identityMock.Object, logger.Object);
    }

    private UpdateUserCommand Command(string name = "New Name", string role = "operator")
        => new(_id, name, role);

    private UserDto Snapshot(string name = "Old Name", string role = "player")
        => new(_id, _id.ToString(), "user@umbral.io", name, role, DateTime.UtcNow);

    private void SetupSnapshot(UserDto? dto)
    {
        _identityMock
            .Setup(s => s.GetUserByIdAsync(_id.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
    }

    // ─── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_HappyPath_UpdatesKeycloakNameThenRoleAndPublishesEvent()
    {
        var cmd = Command();
        SetupSnapshot(Snapshot());

        _identityMock.Setup(s => s.UpdateUserAsync(_id.ToString(), cmd.Name, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _identityMock.Setup(s => s.UpdateUserRoleAsync(_id.ToString(), cmd.Role, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _eventBusMock.Setup(e => e.PublishAsync(It.IsAny<UserUpdated>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _sut.Handle(cmd, CancellationToken.None);

        _identityMock.Verify(s => s.UpdateUserAsync(_id.ToString(), cmd.Name, It.IsAny<CancellationToken>()), Times.Once);
        _identityMock.Verify(s => s.UpdateUserRoleAsync(_id.ToString(), cmd.Role, It.IsAny<CancellationToken>()), Times.Once);
        _eventBusMock.Verify(e => e.PublishAsync(It.IsAny<UserUpdated>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_HappyPath_PublishesEventWithCorrectData()
    {
        var cmd = Command();
        SetupSnapshot(Snapshot());
        UserUpdated? captured = null;

        _identityMock.Setup(s => s.UpdateUserAsync(_id.ToString(), cmd.Name, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _identityMock.Setup(s => s.UpdateUserRoleAsync(_id.ToString(), cmd.Role, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _eventBusMock
            .Setup(e => e.PublishAsync(It.IsAny<UserUpdated>(), It.IsAny<CancellationToken>()))
            .Callback<UserUpdated, CancellationToken>((evt, _) => captured = evt)
            .Returns(Task.CompletedTask);

        await _sut.Handle(cmd, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.UserId.Should().Be(_id);
        captured.KeycloakUserId.Should().Be(_id.ToString());
        captured.Name.Should().Be(cmd.Name);
        captured.Role.Should().Be(cmd.Role);
    }

    // ─── Not found ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenUserNotFoundInKeycloak_ThrowsKeyNotFoundWithoutMutating()
    {
        var cmd = Command();
        SetupSnapshot(null);

        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _identityMock.Verify(s => s.UpdateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _identityMock.Verify(s => s.UpdateUserRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _eventBusMock.Verify(e => e.PublishAsync(It.IsAny<UserUpdated>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── Compensacion ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenRoleUpdateFails_CompensatesByRestoringPreviousNameAndRethrows()
    {
        var cmd = Command(name: "New Name", role: "admin");
        SetupSnapshot(Snapshot(name: "Old Name", role: "player"));

        _identityMock.Setup(s => s.UpdateUserAsync(_id.ToString(), "New Name", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _identityMock.Setup(s => s.UpdateUserRoleAsync(_id.ToString(), "admin", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Keycloak role update failed"));
        _identityMock.Setup(s => s.UpdateUserAsync(_id.ToString(), "Old Name", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*role update failed*");

        _identityMock.Verify(s => s.UpdateUserAsync(_id.ToString(), "New Name", It.IsAny<CancellationToken>()), Times.Once);
        _identityMock.Verify(s => s.UpdateUserAsync(_id.ToString(), "Old Name", It.IsAny<CancellationToken>()), Times.Once);
        _eventBusMock.Verify(e => e.PublishAsync(It.IsAny<UserUpdated>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
