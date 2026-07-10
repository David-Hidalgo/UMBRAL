using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using ScoringService.Application.Abstractions;
using ScoringService.Application.Commands;
using ScoringService.Application.DTOs;
using ScoringService.Application.Handlers;
using ScoringService.Application.IntegrationEvents;
using ScoringService.Domain.Entities;
using ScoringService.Domain.Enums;
using ScoringService.Domain.Exceptions;
using ScoringService.Domain.Repositories;
using ScoringService.Domain.ValueObjects;
using Xunit;

namespace ScoringService.Tests.Application
{
    public class SubmitEvidenceCommandHandlerTests
    {
        private readonly Mock<IScoringRepository> _scoringRepo = new();
        private readonly Mock<ISessionMonitorRepository> _monitorRepo = new();
        private readonly Mock<IMissionContentClient> _missionClient = new();
        private readonly Mock<IScoreBroadcaster> _broadcaster = new();
        private readonly Mock<IEventBus> _eventBus = new();
        private readonly Mock<IScoringNotificationService> _notification = new();
        private readonly SubmitEvidenceCommandHandler _handler;

        private readonly Guid _sessionId = Guid.NewGuid();
        private readonly Guid _missionId = Guid.NewGuid();
        private readonly Guid _teamId = Guid.NewGuid();
        private readonly Guid _subStageId = Guid.NewGuid();

        public SubmitEvidenceCommandHandlerTests()
        {
            _handler = new SubmitEvidenceCommandHandler(
                _scoringRepo.Object, _monitorRepo.Object, _missionClient.Object,
                _broadcaster.Object, _eventBus.Object, _notification.Object);
        }

        private SessionMonitor ActiveMonitorWithTeam()
        {
            var monitor = SessionMonitor.Create(_sessionId, _missionId, "Sesión");
            monitor.Activate(new List<Guid> { _subStageId });
            monitor.RegisterTeam(_teamId, new TeamCode("Alfa"));
            return monitor;
        }

        private void SetupMonitor(SessionMonitor monitor)
            => _monitorRepo.Setup(r => r.GetBySessionIdAsync(_sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(monitor);

        private SubmitEvidenceCommand Command(SubmissionType type, string payload)
            => new(_sessionId, _teamId, _subStageId, type, payload, "Bearer token");

        [Fact]
        public async Task Handle_WhenSessionUnknown_ShouldThrowSessionMonitorNotFound()
        {
            _monitorRepo.Setup(r => r.GetBySessionIdAsync(_sessionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((SessionMonitor?)null);

            Func<Task> act = () => _handler.Handle(Command(SubmissionType.Trivia, "x"), CancellationToken.None);

            await act.Should().ThrowAsync<SessionMonitorNotFoundException>();
        }

        [Fact]
        public async Task Handle_WhenSessionNotActive_ShouldThrowRb03()
        {
            var monitor = SessionMonitor.Create(_sessionId, _missionId, "Sesión"); // Pending
            monitor.RegisterTeam(_teamId, new TeamCode("Alfa"));
            SetupMonitor(monitor);

            Func<Task> act = () => _handler.Handle(Command(SubmissionType.Trivia, "x"), CancellationToken.None);

            await act.Should().ThrowAsync<SessionNotAcceptingEvidenceException>();
        }

        [Fact]
        public async Task Handle_WhenTeamNotInSession_ShouldThrow()
        {
            var monitor = SessionMonitor.Create(_sessionId, _missionId, "Sesión");
            monitor.Activate(new List<Guid> { _subStageId });
            SetupMonitor(monitor);

            Func<Task> act = () => _handler.Handle(Command(SubmissionType.Trivia, "x"), CancellationToken.None);

            await act.Should().ThrowAsync<TeamNotInSessionException>();
        }

        [Fact]
        public async Task Handle_WithCorrectTrivia_ShouldAwardPointsAndBroadcast()
        {
            SetupMonitor(ActiveMonitorWithTeam());
            var answerId = Guid.NewGuid();
            _missionClient.Setup(c => c.GetSubStageContentAsync(_missionId, _subStageId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SubStageContent(_subStageId, SubmissionType.Trivia, 100, new List<Guid> { answerId }, null));
            _broadcaster.Setup(b => b.BroadcastAsync(_sessionId, _teamId, 100, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(100);

            var result = await _handler.Handle(Command(SubmissionType.Trivia, answerId.ToString()), CancellationToken.None);

            result.Status.Should().Be(nameof(SubmissionStatus.Correct));
            result.AwardedPoints.Should().Be(100);
            result.TeamTotalPoints.Should().Be(100);
            _scoringRepo.Verify(r => r.AddScoreEntryAsync(It.IsAny<ScoreEntry>(), It.IsAny<CancellationToken>()), Times.Once);
            _scoringRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _eventBus.Verify(e => e.PublishAsync(It.IsAny<EvidenceSubmittedIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Once);
            _broadcaster.Verify(b => b.BroadcastAsync(_sessionId, _teamId, 100, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
            _notification.Verify(n => n.NotifyEvidenceValidatedAsync(_sessionId, _teamId, It.IsAny<Guid>(), true, 100, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithIncorrectTrivia_ShouldNotAwardOrBroadcast()
        {
            SetupMonitor(ActiveMonitorWithTeam());
            _missionClient.Setup(c => c.GetSubStageContentAsync(_missionId, _subStageId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SubStageContent(_subStageId, SubmissionType.Trivia, 100, new List<Guid> { Guid.NewGuid() }, null));
            _scoringRepo.Setup(r => r.GetScoreEntriesByTeamAsync(_sessionId, _teamId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ScoreEntry>());

            var result = await _handler.Handle(Command(SubmissionType.Trivia, Guid.NewGuid().ToString()), CancellationToken.None);

            result.Status.Should().Be(nameof(SubmissionStatus.Incorrect));
            result.AwardedPoints.Should().Be(0);
            _scoringRepo.Verify(r => r.AddScoreEntryAsync(It.IsAny<ScoreEntry>(), It.IsAny<CancellationToken>()), Times.Never);
            _broadcaster.Verify(b => b.BroadcastAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithCorrectTreasureHunt_ShouldNormalizeAndAward()
        {
            SetupMonitor(ActiveMonitorWithTeam());
            _missionClient.Setup(c => c.GetSubStageContentAsync(_missionId, _subStageId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SubStageContent(_subStageId, SubmissionType.TreasureHunt, 50, new List<Guid>(), "ABC123"));
            _broadcaster.Setup(b => b.BroadcastAsync(_sessionId, _teamId, 50, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(50);

            var result = await _handler.Handle(Command(SubmissionType.TreasureHunt, "  abc123 "), CancellationToken.None);

            result.Status.Should().Be(nameof(SubmissionStatus.Correct));
            result.AwardedPoints.Should().Be(50);
        }

        [Fact]
        public async Task Handle_WhenContentUnavailable_ShouldLeavePending()
        {
            SetupMonitor(ActiveMonitorWithTeam());
            _missionClient.Setup(c => c.GetSubStageContentAsync(_missionId, _subStageId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SubStageContent?)null);
            _scoringRepo.Setup(r => r.GetScoreEntriesByTeamAsync(_sessionId, _teamId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ScoreEntry>());

            var result = await _handler.Handle(Command(SubmissionType.Trivia, Guid.NewGuid().ToString()), CancellationToken.None);

            result.Status.Should().Be(nameof(SubmissionStatus.Pending));
            _broadcaster.Verify(b => b.BroadcastAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
