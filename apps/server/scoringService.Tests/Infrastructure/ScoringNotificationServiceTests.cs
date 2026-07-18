using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using ScoringService.Application.DTOs;
using ScoringService.Infrastructure.Notifications;
using ScoringService.Infrastructure.Notifications.Hubs;
using Xunit;

namespace ScoringService.Tests.Infrastructure
{
    public class ScoringNotificationServiceTests
    {
        private readonly Mock<IHubContext<ScoringHub>> _hubContextMock;
        private readonly Mock<IHubClients> _hubClientsMock;
        private readonly Mock<IClientProxy> _clientProxyMock;
        private readonly ScoringNotificationService _sut;

        public ScoringNotificationServiceTests()
        {
            _hubContextMock = new Mock<IHubContext<ScoringHub>>(MockBehavior.Strict);
            _hubClientsMock = new Mock<IHubClients>(MockBehavior.Strict);
            _clientProxyMock = new Mock<IClientProxy>(MockBehavior.Strict);

            _hubContextMock.Setup(h => h.Clients).Returns(_hubClientsMock.Object);

            _sut = new ScoringNotificationService(_hubContextMock.Object);
        }

        [Fact]
        public async Task NotifyRankingUpdatedAsync_SendsRankingUpdatedToSessionGroup()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var ranking = new List<RankingEntryDto>
            {
                new RankingEntryDto(1, Guid.NewGuid(), "Team Alpha", 150, DateTime.UtcNow)
            };
            var expectedGroup = ScoringHub.SessionGroup(sessionId);

            _hubClientsMock
                .Setup(c => c.Group(expectedGroup))
                .Returns(_clientProxyMock.Object);

            _clientProxyMock
                .Setup(c => c.SendCoreAsync("RankingUpdated", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _sut.NotifyRankingUpdatedAsync(sessionId, ranking, CancellationToken.None);

            // Assert
            _hubClientsMock.Verify(c => c.Group(expectedGroup), Times.Once);
            _clientProxyMock.Verify(c => c.SendCoreAsync("RankingUpdated", It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task NotifyScoreUpdatedAsync_SendsScoreUpdatedToTeamGroup()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            var totalPoints = 250;
            var expectedGroup = ScoringHub.TeamGroup(sessionId, teamId);

            _hubClientsMock
                .Setup(c => c.Group(expectedGroup))
                .Returns(_clientProxyMock.Object);

            _clientProxyMock
                .Setup(c => c.SendCoreAsync("ScoreUpdated", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _sut.NotifyScoreUpdatedAsync(sessionId, teamId, totalPoints, CancellationToken.None);

            // Assert
            _hubClientsMock.Verify(c => c.Group(expectedGroup), Times.Once);
            _clientProxyMock.Verify(c => c.SendCoreAsync("ScoreUpdated", It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task NotifyEvidenceValidatedAsync_SendsEvidenceValidatedToTeamGroup()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            var submissionId = Guid.NewGuid();
            var expectedGroup = ScoringHub.TeamGroup(sessionId, teamId);

            _hubClientsMock
                .Setup(c => c.Group(expectedGroup))
                .Returns(_clientProxyMock.Object);

            _clientProxyMock
                .Setup(c => c.SendCoreAsync("EvidenceValidated", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _sut.NotifyEvidenceValidatedAsync(sessionId, teamId, submissionId, isCorrect: true, awardedPoints: 100, CancellationToken.None);

            // Assert
            _hubClientsMock.Verify(c => c.Group(expectedGroup), Times.Once);
            _clientProxyMock.Verify(c => c.SendCoreAsync("EvidenceValidated", It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
