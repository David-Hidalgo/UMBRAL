using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;
using LiveSessionService.Infrastructure.Notifications;
using LiveSessionService.Infrastructure.Notifications.Hubs;

namespace LiveSessionService.Tests.Infrastructure
{
    public class SessionNotificationServiceTests
    {
        private readonly Mock<IHubContext<LiveSessionHub>> _hubContextMock = new();
        private readonly Mock<IHubClients> _clientsMock = new();
        private readonly Mock<IClientProxy> _clientProxyMock = new();
        private readonly SessionNotificationService _service;

        public SessionNotificationServiceTests()
        {
            _hubContextMock.Setup(h => h.Clients).Returns(_clientsMock.Object);
            _clientsMock
                .Setup(c => c.Group(It.IsAny<string>()))
                .Returns(_clientProxyMock.Object);

            _service = new SessionNotificationService(_hubContextMock.Object);
        }

        [Fact]
        public async Task NotifyPlayerJoinedAsync_Should_Send_To_Session_Group()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var teamId = Guid.NewGuid();

            // Act
            await _service.NotifyPlayerJoinedAsync(sessionId, teamId, "Ana", CancellationToken.None);

            // Assert
            _clientsMock.Verify(c => c.Group($"session-{sessionId}"), Times.Once);
            _clientProxyMock.Verify(
                p => p.SendCoreAsync(
                    "PlayerJoined",
                    It.Is<object[]>(args => args.Length == 1),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task NotifyStateChangedAsync_Should_Send_To_Session_Group()
        {
            // Arrange
            var sessionId = Guid.NewGuid();

            // Act
            await _service.NotifyStateChangedAsync(sessionId, "Activa", null, null, CancellationToken.None);

            // Assert
            _clientsMock.Verify(c => c.Group($"session-{sessionId}"), Times.Once);
            _clientProxyMock.Verify(
                p => p.SendCoreAsync(
                    "StateChanged",
                    It.Is<object[]>(args => args.Length == 1),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task NotifyStageAdvancedAsync_Should_Send_To_Session_Group()
        {
            // Arrange
            var sessionId = Guid.NewGuid();

            // Act
            await _service.NotifyStageAdvancedAsync(sessionId, 2, CancellationToken.None);

            // Assert
            _clientsMock.Verify(c => c.Group($"session-{sessionId}"), Times.Once);
            _clientProxyMock.Verify(
                p => p.SendCoreAsync(
                    "StageAdvanced",
                    It.Is<object[]>(args => args.Length == 1),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task NotifyHintRequestedAsync_Should_Send_To_Session_Group()
        {
            // Arrange: la solicitud del jugador llega al operador (grupo de la sesión).
            var sessionId = Guid.NewGuid();
            var teamId = Guid.NewGuid();

            // Act
            await _service.NotifyHintRequestedAsync(sessionId, Guid.NewGuid(), teamId, "Alfa", Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);

            // Assert
            _clientsMock.Verify(c => c.Group($"session-{sessionId}"), Times.Once);
            _clientProxyMock.Verify(
                p => p.SendCoreAsync(
                    "HintRequested",
                    It.Is<object[]>(args => args.Length == 1),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task NotifyHintReleasedAsync_Should_Send_Only_To_Team_Group()
        {
            // Arrange: la pista liberada va SOLO al grupo del equipo destinatario.
            var sessionId = Guid.NewGuid();
            var teamId = Guid.NewGuid();

            // Act
            await _service.NotifyHintReleasedAsync(sessionId, teamId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Pista", 1, DateTime.UtcNow, CancellationToken.None);

            // Assert
            _clientsMock.Verify(c => c.Group($"team-{teamId}"), Times.Once);
            _clientsMock.Verify(c => c.Group($"session-{sessionId}"), Times.Never);
            _clientProxyMock.Verify(
                p => p.SendCoreAsync(
                    "HintReleased",
                    It.Is<object[]>(args => args.Length == 1),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task NotifyHintRequestResolvedAsync_Should_Send_To_Session_Group()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var teamId = Guid.NewGuid();

            // Act
            await _service.NotifyHintRequestResolvedAsync(sessionId, teamId, Guid.NewGuid(), CancellationToken.None);

            // Assert
            _clientsMock.Verify(c => c.Group($"session-{sessionId}"), Times.Once);
            _clientProxyMock.Verify(
                p => p.SendCoreAsync(
                    "HintRequestResolved",
                    It.Is<object[]>(args => args.Length == 1),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
