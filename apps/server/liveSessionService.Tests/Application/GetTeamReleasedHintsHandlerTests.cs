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
using LiveSessionService.Tests.Support;

namespace LiveSessionService.Tests.Application
{
    public class GetTeamReleasedHintsHandlerTests
    {
        private const string MemberUserId = "user-member";
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

            Func<Task> act = () => _handler.Handle(
                new GetTeamReleasedHintsQuery(Guid.NewGuid(), Guid.NewGuid(), "user-1", true), CancellationToken.None);

            await act.Should().ThrowAsync<BusinessException>()
                .WithMessage("La sesión de juego especificada no existe.");
        }

        [Fact]
        public async Task Handle_WhenPrivileged_ShouldReturnOnlyRequestedTeamHints()
        {
            var (session, teamA, _) = CreatePreparingSessionWithTwoTeams();
            session.Activate();
            session.GrantHint(teamA.Id, Guid.NewGuid(), Guid.NewGuid(), "Pista A", 1, DateTime.UtcNow);

            SetupSession(session);

            // Un operador (privilegiado) puede consultar las pistas de cualquier equipo.
            var result = await _handler.Handle(
                new GetTeamReleasedHintsQuery(session.Id, teamA.Id, "operator-1", true), CancellationToken.None);

            result.Should().ContainSingle();
            result[0].TeamId.Should().Be(teamA.Id);
            result[0].Text.Should().Be("Pista A");
        }

        [Fact]
        public async Task Handle_WhenMemberOfTeam_ShouldReturnHints()
        {
            var (session, teamA, _) = CreatePreparingSessionWithTwoTeams();
            session.AddPlayerToTeam(teamA.Id, Player.Create(Guid.NewGuid(), MemberUserId, "Ana", teamA.Id));
            session.Activate();
            session.GrantHint(teamA.Id, Guid.NewGuid(), Guid.NewGuid(), "Pista A", 1, DateTime.UtcNow);

            SetupSession(session);

            // Un jugador miembro del equipo (no privilegiado) sí puede ver sus propias pistas.
            var result = await _handler.Handle(
                new GetTeamReleasedHintsQuery(session.Id, teamA.Id, MemberUserId, false), CancellationToken.None);

            result.Should().ContainSingle();
            result[0].Text.Should().Be("Pista A");
        }

        [Fact]
        public async Task Handle_WhenNotMemberAndNotPrivileged_ShouldThrowBusinessException()
        {
            var (session, teamA, teamB) = CreatePreparingSessionWithTwoTeams();
            // El usuario es miembro del equipo B, pero intenta leer las pistas del equipo A.
            session.AddPlayerToTeam(teamB.Id, Player.Create(Guid.NewGuid(), "user-rival", "Beto", teamB.Id));
            session.Activate();
            session.GrantHint(teamA.Id, Guid.NewGuid(), Guid.NewGuid(), "Pista A", 1, DateTime.UtcNow);

            SetupSession(session);

            Func<Task> act = () => _handler.Handle(
                new GetTeamReleasedHintsQuery(session.Id, teamA.Id, "user-rival", false), CancellationToken.None);

            await act.Should().ThrowAsync<BusinessException>()
                .WithMessage("No tienes permiso para consultar las pistas de este equipo.");
        }

        private void SetupSession(LiveSession session)
        {
            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
        }

        // Sesión en preparación con dos equipos registrados (aún permite agregar jugadores).
        private static (LiveSession session, Team teamA, Team teamB) CreatePreparingSessionWithTwoTeams()
        {
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            session.StartPreparation();
            var teamA = session.RegisterTeamWithPlayer("Alfa");
            var teamB = session.RegisterTeamWithPlayer("Beta");
            return (session, teamA, teamB);
        }
    }
}
