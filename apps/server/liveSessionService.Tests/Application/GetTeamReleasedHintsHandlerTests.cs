using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using LiveSessionService.Application.Handlers;
using LiveSessionService.Application.Queries;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.Repositories;

namespace LiveSessionService.Tests.Application
{
    public class GetTeamReleasedHintsHandlerTests
    {
        private readonly Mock<ILiveSessionRepository> _repositoryMock = new();
        private readonly GetTeamReleasedHintsHandler _handler;

        public GetTeamReleasedHintsHandlerTests()
        {
            _handler = new GetTeamReleasedHintsHandler(_repositoryMock.Object);
        }

        [Fact]
        public async Task Handle_WhenSessionDoesNotExist_ShouldThrowBusinessException()
        {
            _repositoryMock
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((LiveSession?)null);

            Func<Task> act = () => _handler.Handle(new GetTeamReleasedHintsQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

            await act.Should().ThrowAsync<BusinessException>()
                .WithMessage("La sesión de juego especificada no existe.");
        }

        [Fact]
        public async Task Handle_ShouldReturnOnlyRequestedTeamHints()
        {
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            session.StartPreparation();
            var teamA = Team.Create(Guid.NewGuid(), "Alfa", session.Id);
            var teamB = Team.Create(Guid.NewGuid(), "Beta", session.Id);
            session.RegisterTeam(teamA);
            session.RegisterTeam(teamB);
            session.Activate();

            var subStageId = Guid.NewGuid();
            session.GrantHint(teamA.Id, subStageId, Guid.NewGuid(), "Pista A", 1, DateTime.UtcNow);
            session.GrantHint(teamB.Id, subStageId, Guid.NewGuid(), "Pista B", 1, DateTime.UtcNow);

            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var result = await _handler.Handle(new GetTeamReleasedHintsQuery(session.Id, teamA.Id), CancellationToken.None);

            result.Should().ContainSingle();
            result[0].TeamId.Should().Be(teamA.Id);
            result[0].Text.Should().Be("Pista A");
        }
    }
}
