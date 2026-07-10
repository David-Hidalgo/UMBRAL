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
using ScoringService.Domain.Entities;
using ScoringService.Domain.Enums;
using ScoringService.Domain.Exceptions;
using ScoringService.Domain.Repositories;
using ScoringService.Domain.ValueObjects;
using Xunit;

namespace ScoringService.Tests.Application
{
    public class ValidateEvidenceCommandHandlerTests
    {
        private readonly Mock<IScoringRepository> _scoringRepo = new();
        private readonly Mock<ISessionMonitorRepository> _monitorRepo = new();
        private readonly Mock<IMissionContentClient> _missionClient = new();
        private readonly Mock<IScoreBroadcaster> _broadcaster = new();
        private readonly Mock<IScoringNotificationService> _notification = new();
        private readonly ValidateEvidenceCommandHandler _handler;

        private readonly Guid _sessionId = Guid.NewGuid();
        private readonly Guid _missionId = Guid.NewGuid();
        private readonly Guid _teamId = Guid.NewGuid();
        private readonly Guid _subStageId = Guid.NewGuid();

        public ValidateEvidenceCommandHandlerTests()
        {
            _handler = new ValidateEvidenceCommandHandler(
                _scoringRepo.Object, _monitorRepo.Object, _missionClient.Object,
                _broadcaster.Object, _notification.Object);
        }

        private EvidenceSubmission PendingSubmission()
            => EvidenceSubmission.Create(Guid.NewGuid(), _sessionId, _teamId, _subStageId, SubmissionType.TreasureHunt, "codigo", DateTime.UtcNow);

        private void SetupMonitor()
        {
            var monitor = SessionMonitor.Create(_sessionId, _missionId, "S");
            monitor.Activate(new List<Guid> { _subStageId });
            monitor.RegisterTeam(_teamId, new TeamCode("Alfa"));
            _monitorRepo.Setup(r => r.GetBySessionIdAsync(_sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(monitor);
        }

        [Fact]
        public async Task Handle_WhenSubmissionMissing_ShouldThrow()
        {
            _scoringRepo.Setup(r => r.GetSubmissionByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((EvidenceSubmission?)null);

            var command = new ValidateEvidenceCommand(_sessionId, Guid.NewGuid(), true, "op", "Bearer t");

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<SubmissionNotFoundException>();
        }

        [Fact]
        public async Task Handle_WhenValidatedAsCorrect_ShouldAwardStagePoints()
        {
            var submission = PendingSubmission();
            _scoringRepo.Setup(r => r.GetSubmissionByIdAsync(submission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(submission);
            SetupMonitor();
            _missionClient.Setup(c => c.GetSubStageContentAsync(_missionId, _subStageId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SubStageContent(_subStageId, SubmissionType.TreasureHunt, 70, new List<Guid>(), "ABC"));
            _broadcaster.Setup(b => b.BroadcastAsync(_sessionId, _teamId, 70, It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(70);

            var command = new ValidateEvidenceCommand(_sessionId, submission.Id, true, "op", "Bearer t");
            var result = await _handler.Handle(command, CancellationToken.None);

            result.Status.Should().Be(nameof(SubmissionStatus.Correct));
            result.AwardedPoints.Should().Be(70);
            _scoringRepo.Verify(r => r.AddScoreEntryAsync(It.IsAny<ScoreEntry>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenValidatedAsIncorrect_ShouldNotAward()
        {
            var submission = PendingSubmission();
            _scoringRepo.Setup(r => r.GetSubmissionByIdAsync(submission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(submission);
            SetupMonitor();
            _scoringRepo.Setup(r => r.GetScoreEntriesByTeamAsync(_sessionId, _teamId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ScoreEntry>());

            var command = new ValidateEvidenceCommand(_sessionId, submission.Id, false, "op", "Bearer t");
            var result = await _handler.Handle(command, CancellationToken.None);

            result.Status.Should().Be(nameof(SubmissionStatus.Incorrect));
            _scoringRepo.Verify(r => r.AddScoreEntryAsync(It.IsAny<ScoreEntry>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    public class ApplyPenaltyCommandHandlerTests
    {
        private readonly Mock<IScoringRepository> _scoringRepo = new();
        private readonly Mock<ISessionMonitorRepository> _monitorRepo = new();
        private readonly Mock<IScoreBroadcaster> _broadcaster = new();
        private readonly ApplyPenaltyCommandHandler _handler;

        private readonly Guid _sessionId = Guid.NewGuid();
        private readonly Guid _teamId = Guid.NewGuid();

        public ApplyPenaltyCommandHandlerTests()
        {
            _handler = new ApplyPenaltyCommandHandler(_scoringRepo.Object, _monitorRepo.Object, _broadcaster.Object);
        }

        private void SetupMonitorWithTeam()
        {
            var monitor = SessionMonitor.Create(_sessionId, Guid.NewGuid(), "S");
            monitor.RegisterTeam(_teamId, new TeamCode("Alfa"));
            _monitorRepo.Setup(r => r.GetBySessionIdAsync(_sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(monitor);
        }

        [Fact]
        public async Task Handle_WhenTeamNotInSession_ShouldThrow()
        {
            var monitor = SessionMonitor.Create(_sessionId, Guid.NewGuid(), "S");
            _monitorRepo.Setup(r => r.GetBySessionIdAsync(_sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(monitor);

            var command = new ApplyPenaltyCommand(_sessionId, _teamId, 20, "Retraso", "op");

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<TeamNotInSessionException>();
        }

        [Fact]
        public async Task Handle_WithValidPenalty_ShouldRecordNegativeEntryAndBroadcast()
        {
            SetupMonitorWithTeam();

            var command = new ApplyPenaltyCommand(_sessionId, _teamId, 20, "Retraso", "op");
            await _handler.Handle(command, CancellationToken.None);

            _scoringRepo.Verify(r => r.AddScoreEntryAsync(
                It.Is<ScoreEntry>(e => e.Value.Points == -20 && e.Reason!.Value == "Retraso"),
                It.IsAny<CancellationToken>()), Times.Once);
            _scoringRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _broadcaster.Verify(b => b.BroadcastAsync(_sessionId, _teamId, -20, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
