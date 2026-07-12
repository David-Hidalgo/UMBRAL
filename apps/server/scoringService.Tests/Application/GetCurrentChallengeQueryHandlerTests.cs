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
using Xunit;

namespace ScoringService.Tests.Application
{
    public class GetCurrentChallengeQueryHandlerTests
    {
        private readonly Mock<ISessionMonitorRepository> _monitorRepo = new();
        private readonly Mock<IMissionContentClient> _missionClient = new();
        private readonly Mock<IScoringRepository> _scoringRepo = new();

        private GetCurrentChallengeQueryHandler CreateHandler()
            => new(_monitorRepo.Object, _missionClient.Object, _scoringRepo.Object);

        [Fact]
        public async Task Handle_WhenSessionMissing_ShouldThrow()
        {
            _monitorRepo.Setup(r => r.GetBySessionIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SessionMonitor?)null);

            Func<Task> act = () => CreateHandler().Handle(new GetCurrentChallengeQuery(Guid.NewGuid()), CancellationToken.None);

            await act.Should().ThrowAsync<SessionMonitorNotFoundException>();
        }

        [Fact]
        public async Task Handle_WithTeam_ShouldMarkClosedSubStagesByType()
        {
            var sessionId = Guid.NewGuid();
            var missionId = Guid.NewGuid();
            var triviaId = Guid.NewGuid(); // Trivia respondida (1 intento) → cerrada
            var treasureId = Guid.NewGuid(); // Búsqueda no resuelta → abierta

            var monitor = SessionMonitor.Create(sessionId, missionId, "Sesión");
            monitor.Activate(new List<Guid> { triviaId, treasureId });
            _monitorRepo.Setup(r => r.GetBySessionIdAsync(sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(monitor);

            _missionClient
                .Setup(c => c.GetPlayerSubStagesAsync(missionId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PlayerSubStageDto>
                {
                    new(triviaId, "Reto 1", 100, SubmissionType.Trivia, null, "obj"),
                    new(treasureId, "Reto 2", 50, SubmissionType.TreasureHunt, null, "obj"),
                });

            var teamId = Guid.NewGuid();
            // Trivia: cierra por cualquier intento. Búsqueda: cierra solo al resolver (Correct).
            _scoringRepo
                .Setup(r => r.GetSubmissionsForSubStagesAsync(sessionId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SubStageSubmissionInfo>
                {
                    new(teamId, triviaId, SubmissionStatus.Incorrect),
                });

            var result = await CreateHandler().Handle(new GetCurrentChallengeQuery(sessionId, teamId), CancellationToken.None);

            result.SubStages.Should().HaveCount(2);
            result.SubStages.First(s => s.Id == triviaId).Closed.Should().BeTrue();
            result.SubStages.First(s => s.Id == treasureId).Closed.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_ShouldReturnActiveSanitizedSubStages()
        {
            var sessionId = Guid.NewGuid();
            var missionId = Guid.NewGuid();
            var subId = Guid.NewGuid();

            var monitor = SessionMonitor.Create(sessionId, missionId, "Sesión");
            monitor.Activate(new List<Guid> { subId });
            _monitorRepo.Setup(r => r.GetBySessionIdAsync(sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(monitor);

            var sanitized = new List<PlayerSubStageDto>
            {
                new(subId, "Reto", 100, SubmissionType.Trivia,
                    new PlayerQuestionDto(Guid.NewGuid(), "¿Capital?",
                        new List<PlayerAnswerDto> { new(Guid.NewGuid(), "París"), new(Guid.NewGuid(), "Londres") }),
                    null),
            };
            _missionClient
                .Setup(c => c.GetPlayerSubStagesAsync(missionId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sanitized);

            var result = await CreateHandler().Handle(new GetCurrentChallengeQuery(sessionId), CancellationToken.None);

            result.Status.Should().Be(nameof(SessionMonitorStatus.Active));
            result.SubStages.Should().ContainSingle();
            result.SubStages[0].Question!.Answers.Should().HaveCount(2);
        }
    }
}
