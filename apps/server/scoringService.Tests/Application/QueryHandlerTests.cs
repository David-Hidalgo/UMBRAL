using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using ScoringService.Application.Handlers;
using ScoringService.Application.Queries;
using ScoringService.Domain.Entities;
using ScoringService.Domain.Exceptions;
using ScoringService.Domain.Repositories;
using ScoringService.Domain.ValueObjects;
using Xunit;

namespace ScoringService.Tests.Application
{
    public class GetSessionRankingQueryHandlerTests
    {
        private readonly Mock<IScoringRepository> _scoringRepo = new();
        private readonly Mock<ISessionMonitorRepository> _monitorRepo = new();

        [Fact]
        public async Task Handle_WhenSessionMissing_ShouldThrow()
        {
            _monitorRepo.Setup(r => r.GetBySessionIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SessionMonitor?)null);
            var handler = new GetSessionRankingQueryHandler(_scoringRepo.Object, _monitorRepo.Object);

            Func<Task> act = () => handler.Handle(new GetSessionRankingQuery(Guid.NewGuid()), CancellationToken.None);

            await act.Should().ThrowAsync<SessionMonitorNotFoundException>();
        }

        [Fact]
        public async Task Handle_ShouldReturnRankingForAllTeams()
        {
            var sessionId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            var monitor = SessionMonitor.Create(sessionId, Guid.NewGuid(), "S");
            monitor.RegisterTeam(teamId, new TeamCode("Alfa"));
            _monitorRepo.Setup(r => r.GetBySessionIdAsync(sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(monitor);
            _scoringRepo.Setup(r => r.GetScoreEntriesBySessionAsync(sessionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ScoreEntry>
                {
                    ScoreEntry.ForEvidence(Guid.NewGuid(), sessionId, teamId, ScoreValue.Award(30), Guid.NewGuid(), DateTime.UtcNow)
                });
            var handler = new GetSessionRankingQueryHandler(_scoringRepo.Object, _monitorRepo.Object);

            var ranking = await handler.Handle(new GetSessionRankingQuery(sessionId), CancellationToken.None);

            ranking.Should().ContainSingle();
            ranking[0].TotalPoints.Should().Be(30);
        }
    }

    public class GetTeamScoreQueryHandlerTests
    {
        private readonly Mock<IScoringRepository> _scoringRepo = new();
        private readonly Mock<ISessionMonitorRepository> _monitorRepo = new();

        [Fact]
        public async Task Handle_WhenTeamNotInSession_ShouldThrow()
        {
            var sessionId = Guid.NewGuid();
            var monitor = SessionMonitor.Create(sessionId, Guid.NewGuid(), "S");
            _monitorRepo.Setup(r => r.GetBySessionIdAsync(sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(monitor);
            var handler = new GetTeamScoreQueryHandler(_scoringRepo.Object, _monitorRepo.Object);

            Func<Task> act = () => handler.Handle(new GetTeamScoreQuery(sessionId, Guid.NewGuid()), CancellationToken.None);

            await act.Should().ThrowAsync<TeamNotInSessionException>();
        }

        [Fact]
        public async Task Handle_ShouldReturnTeamTotal()
        {
            var sessionId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            var monitor = SessionMonitor.Create(sessionId, Guid.NewGuid(), "S");
            monitor.RegisterTeam(teamId, new TeamCode("Alfa"));
            _monitorRepo.Setup(r => r.GetBySessionIdAsync(sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(monitor);
            _scoringRepo.Setup(r => r.GetScoreEntriesByTeamAsync(sessionId, teamId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ScoreEntry>
                {
                    ScoreEntry.ForEvidence(Guid.NewGuid(), sessionId, teamId, ScoreValue.Award(40), Guid.NewGuid(), DateTime.UtcNow),
                    ScoreEntry.ForPenalty(Guid.NewGuid(), sessionId, teamId, ScoreValue.Penalty(10), new PenaltyReason("x"), DateTime.UtcNow)
                });
            var handler = new GetTeamScoreQueryHandler(_scoringRepo.Object, _monitorRepo.Object);

            var score = await handler.Handle(new GetTeamScoreQuery(sessionId, teamId), CancellationToken.None);

            score.TotalPoints.Should().Be(30);
            score.TeamCode.Should().Be("Alfa");
        }
    }

    public class GetSessionEventsQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldMapEventsToDtos()
        {
            var sessionId = Guid.NewGuid();
            var scoringRepo = new Mock<IScoringRepository>();
            scoringRepo.Setup(r => r.GetSessionEventsAsync(sessionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SessionEvent>
                {
                    SessionEvent.Record(Guid.NewGuid(), sessionId, "SessionCreated", "desc", DateTime.UtcNow, "op")
                });
            var handler = new GetSessionEventsQueryHandler(scoringRepo.Object);

            var events = await handler.Handle(new GetSessionEventsQuery(sessionId), CancellationToken.None);

            events.Should().ContainSingle();
            events[0].Type.Should().Be("SessionCreated");
            events[0].Actor.Should().Be("op");
        }
    }
}
