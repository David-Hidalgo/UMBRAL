using System;
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
    public class GetLiveSessionByIdHandlerTests
    {
        private readonly Mock<ILiveSessionRepository> _repositoryMock = new();
        private readonly GetLiveSessionByIdHandler _handler;

        public GetLiveSessionByIdHandlerTests()
        {
            _handler = new GetLiveSessionByIdHandler(_repositoryMock.Object);
        }

        [Fact]
        public async Task Handle_WhenSessionNotFound_ShouldReturnNull()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repositoryMock
                .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((LiveSession?)null);

            // Act
            var result = await _handler.Handle(new GetLiveSessionByIdQuery(id), CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task Handle_WhenSessionExists_ShouldMapToDetailDto()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión X", "Partida 1", null);
            var team = Team.Create(Guid.NewGuid(), "Alfa", session.Id);
            session.RegisterTeam(team);
            var player = Player.Create(Guid.NewGuid(), "user-1", "Ana", team.Id);
            session.AddPlayerToTeam(team.Id, player);

            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            // Act
            var result = await _handler.Handle(new GetLiveSessionByIdQuery(session.Id), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(session.Id);
            result.MissionId.Should().Be(session.MissionId);
            result.MissionName.Should().Be("Misión X");
            result.Name.Should().Be("Partida 1");
            result.Status.Should().Be(session.Status.ToString());
            result.Teams.Should().ContainSingle();
            var teamDto = result.Teams.Single();
            teamDto.Name.Should().Be("Alfa");
            teamDto.Players.Should().ContainSingle(p => p.KeycloakUserId == "user-1" && p.DisplayName == "Ana");
        }
    }
}
