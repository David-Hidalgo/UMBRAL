using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using ScoringService.Application.Abstractions;
using ScoringService.Application.Common;
using ScoringService.Application.DTOs;
using ScoringService.Application.IntegrationEvents;
using ScoringService.Domain.Entities;
using ScoringService.Domain.Exceptions;
using ScoringService.Domain.Repositories;
using ScoringService.Domain.ValueObjects;
using Xunit;

namespace ScoringService.Tests.Application
{
    public class ScoreBroadcasterTests
    {
        private readonly Mock<IScoringRepository> _scoringRepo = new();
        private readonly Mock<ISessionMonitorRepository> _monitorRepo = new();
        private readonly Mock<IScoringNotificationService> _notification = new();
        private readonly Mock<IEventBus> _eventBus = new();
        private readonly ScoreBroadcaster _broadcaster;

        public ScoreBroadcasterTests()
        {
            _broadcaster = new ScoreBroadcaster(
                _scoringRepo.Object, _monitorRepo.Object, _notification.Object, _eventBus.Object);
        }

        [Fact]
        public async Task BroadcastAsync_WhenSessionMissing_ShouldThrow()
        {
            _monitorRepo.Setup(r => r.GetBySessionIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SessionMonitor?)null);

            Func<Task> act = () => _broadcaster.BroadcastAsync(Guid.NewGuid(), Guid.NewGuid(), 10, DateTime.UtcNow, CancellationToken.None);

            await act.Should().ThrowAsync<SessionMonitorNotFoundException>();
        }

        [Fact]
        public async Task BroadcastAsync_ShouldComputeTotalNotifyAndPublish()
        {
            var sessionId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            var monitor = SessionMonitor.Create(sessionId, Guid.NewGuid(), "S");
            monitor.RegisterTeam(teamId, new TeamCode("Alfa"));
            _monitorRepo.Setup(r => r.GetBySessionIdAsync(sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(monitor);
            _scoringRepo.Setup(r => r.GetScoreEntriesBySessionAsync(sessionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ScoreEntry>
                {
                    ScoreEntry.ForEvidence(Guid.NewGuid(), sessionId, teamId, ScoreValue.Award(80), Guid.NewGuid(), DateTime.UtcNow)
                });

            var total = await _broadcaster.BroadcastAsync(sessionId, teamId, 80, DateTime.UtcNow, CancellationToken.None);

            total.Should().Be(80);
            _notification.Verify(n => n.NotifyScoreUpdatedAsync(sessionId, teamId, 80, It.IsAny<CancellationToken>()), Times.Once);
            _notification.Verify(n => n.NotifyRankingUpdatedAsync(sessionId, It.IsAny<IReadOnlyList<RankingEntryDto>>(), It.IsAny<CancellationToken>()), Times.Once);
            _eventBus.Verify(e => e.PublishAsync(It.IsAny<ScoreRecalculatedIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
