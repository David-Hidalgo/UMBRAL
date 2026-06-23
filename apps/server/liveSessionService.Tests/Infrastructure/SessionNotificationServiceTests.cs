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
            await _service.NotifyStateChangedAsync(sessionId, "Activa", CancellationToken.None);

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
    }
}
