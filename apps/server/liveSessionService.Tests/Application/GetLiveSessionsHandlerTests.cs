using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using LiveSessionService.Application.Handlers;
using LiveSessionService.Application.Queries;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Repositories;

namespace LiveSessionService.Tests.Application
{
    public class GetLiveSessionsHandlerTests
    {
        private readonly Mock<ILiveSessionRepository> _repositoryMock = new();
        private readonly GetLiveSessionsHandler _handler;

        public GetLiveSessionsHandlerTests()
        {
            _handler = new GetLiveSessionsHandler(_repositoryMock.Object);
        }

        [Fact]
        public async Task Handle_WhenNoSessionsExist_ShouldReturnEmptyList()
        {
            // Arrange
            _repositoryMock
                .Setup(r => r.GetAllAsync(null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<LiveSession>());

            // Act
            var result = await _handler.Handle(new GetLiveSessionsQuery(null, null), CancellationToken.None);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_ShouldMapSessionsToSummaryDtos()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var session = LiveSession.Create(Guid.NewGuid(), missionId, "Misión", "Partida", null);
            var team = Team.Create(Guid.NewGuid(), "Alfa", session.Id);
            session.RegisterTeam(team);
            session.AddPlayerToTeam(team.Id, Player.Create(Guid.NewGuid(), "user-1", "Ana", team.Id));

            _repositoryMock
                .Setup(r => r.GetAllAsync(missionId, "Programada", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<LiveSession> { session });

            // Act
            var result = await _handler.Handle(new GetLiveSessionsQuery(missionId, "Programada"), CancellationToken.None);

            // Assert
            var summary = result.Single();
            summary.Id.Should().Be(session.Id);
            summary.MissionId.Should().Be(missionId);
            summary.Name.Should().Be("Partida");
            summary.TeamCount.Should().Be(1);
            summary.PlayerCount.Should().Be(1);
            summary.Status.Should().Be(session.Status.ToString());

            _repositoryMock.Verify(
                r => r.GetAllAsync(missionId, "Programada", It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
