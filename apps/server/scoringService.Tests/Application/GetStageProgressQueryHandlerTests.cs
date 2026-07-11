using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using ScoringService.Application.Abstractions;
using ScoringService.Application.DTOs;
using ScoringService.Application.Handlers;
using ScoringService.Application.Queries;
using ScoringService.Domain.Entities;
using ScoringService.Domain.Enums;
using ScoringService.Domain.Exceptions;
using ScoringService.Domain.Repositories;
using ScoringService.Domain.ValueObjects;
using Xunit;

namespace ScoringService.Tests.Application
{
    public class GetStageProgressQueryHandlerTests
    {
        private readonly Mock<ISessionMonitorRepository> _monitorRepo = new();
        private readonly Mock<IMissionContentClient> _missionClient = new();
        private readonly Mock<IScoringRepository> _scoringRepo = new();

        private GetStageProgressQueryHandler CreateHandler()
            => new(_monitorRepo.Object, _missionClient.Object, _scoringRepo.Object);

        [Fact]
        public async Task Handle_WhenSessionMissing_ShouldThrow()
        {
            _monitorRepo.Setup(r => r.GetBySessionIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SessionMonitor?)null);

            Func<Task> act = () => CreateHandler().Handle(new GetStageProgressQuery(Guid.NewGuid()), CancellationToken.None);

            await act.Should().ThrowAsync<SessionMonitorNotFoundException>();
        }

        [Fact]
        public async Task Handle_ShouldReportPerTeamCompletion()
        {
            var sessionId = Guid.NewGuid();
            var missionId = Guid.NewGuid();
            var trivia = Guid.NewGuid();
            var treasure = Guid.NewGuid();
            var alfa = Guid.NewGuid();
            var bravo = Guid.NewGuid();

            var monitor = SessionMonitor.Create(sessionId, missionId, "Sesión");
            monitor.Activate(new List<Guid> { trivia, treasure });
            monitor.RegisterTeam(alfa, new TeamCode("Alfa"));
            monitor.RegisterTeam(bravo, new TeamCode("Bravo"));
            _monitorRepo.Setup(r => r.GetBySessionIdAsync(sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(monitor);

            _missionClient
                .Setup(c => c.GetPlayerSubStagesAsync(missionId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PlayerSubStageDto>
                {
                    new(trivia, "T", 100, SubmissionType.Trivia, null, null),
                    new(treasure, "B", 50, SubmissionType.TreasureHunt, null, "obj"),
                });

            // Alfa completó ambas; Bravo solo la trivia.
            _scoringRepo.Setup(r => r.HasAnySubmissionAsync(sessionId, alfa, trivia, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _scoringRepo.Setup(r => r.HasSolvedSubStageAsync(sessionId, alfa, treasure, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _scoringRepo.Setup(r => r.HasAnySubmissionAsync(sessionId, bravo, trivia, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _scoringRepo.Setup(r => r.HasSolvedSubStageAsync(sessionId, bravo, treasure, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var result = await CreateHandler().Handle(new GetStageProgressQuery(sessionId), CancellationToken.None);

            result.SubStageCount.Should().Be(2);
            result.Teams.First(t => t.TeamId == alfa).Completed.Should().Be(2);
            result.Teams.First(t => t.TeamId == bravo).Completed.Should().Be(1);
            result.AllComplete.Should().BeFalse();
        }
    }
}
