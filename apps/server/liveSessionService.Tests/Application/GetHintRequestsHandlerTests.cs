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
    public class GetHintRequestsHandlerTests
    {
        private const string PlayerId = "user-123";
        private readonly Mock<ILiveSessionRepository> _repositoryMock = new();
        private readonly GetHintRequestsHandler _handler;

        public GetHintRequestsHandlerTests()
        {
            _handler = new GetHintRequestsHandler(_repositoryMock.Object);
        }

        [Fact]
        public async Task Handle_WhenSessionDoesNotExist_ShouldThrowBusinessException()
        {
            _repositoryMock
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((LiveSession?)null);

            Func<Task> act = () => _handler.Handle(new GetHintRequestsQuery(Guid.NewGuid()), CancellationToken.None);

            await act.Should().ThrowAsync<BusinessException>()
                .WithMessage("La sesión de juego especificada no existe.");
        }

        [Fact]
        public async Task Handle_ShouldReturnRequestsWithTeamName()
        {
            var (session, team) = CreateActiveSessionWithPlayer();
            session.RequestHint(team.Id, Guid.NewGuid(), PlayerId, DateTime.UtcNow);
            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var result = await _handler.Handle(new GetHintRequestsQuery(session.Id), CancellationToken.None);

            result.Should().ContainSingle();
            result[0].TeamName.Should().Be("Alfa");
            result[0].Status.Should().Be("Pendiente");
        }

        [Fact]
        public async Task Handle_WithStatusFilter_ShouldReturnOnlyMatching()
        {
            var (session, team) = CreateActiveSessionWithPlayer();
            var subStageId = Guid.NewGuid();
            session.RequestHint(team.Id, subStageId, PlayerId, DateTime.UtcNow);
            // Resolver esa solicitud otorgando una pista.
            session.GrantHint(team.Id, subStageId, Guid.NewGuid(), "Pista", 1, DateTime.UtcNow);
            // Y una segunda solicitud que queda Pendiente.
            session.RequestHint(team.Id, Guid.NewGuid(), PlayerId, DateTime.UtcNow);

            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var pendientes = await _handler.Handle(new GetHintRequestsQuery(session.Id, "Pendiente"), CancellationToken.None);
            var resueltas = await _handler.Handle(new GetHintRequestsQuery(session.Id, "Resuelta"), CancellationToken.None);

            pendientes.Should().ContainSingle().Which.Status.Should().Be("Pendiente");
            resueltas.Should().ContainSingle().Which.Status.Should().Be("Resuelta");
        }

        private static (LiveSession session, Team team) CreateActiveSessionWithPlayer()
        {
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            session.StartPreparation();
            var team = Team.Create(Guid.NewGuid(), "Alfa", session.Id);
            session.RegisterTeam(team);
            session.AddPlayerToTeam(team.Id, Player.Create(Guid.NewGuid(), PlayerId, "Ana", team.Id));
            session.Activate();
            return (session, team);
        }
    }
}
