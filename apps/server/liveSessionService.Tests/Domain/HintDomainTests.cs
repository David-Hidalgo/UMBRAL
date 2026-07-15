using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Exceptions;

namespace LiveSessionService.Tests.Domain
{
    public class HintDomainTests
    {
        private const string PlayerId = "user-123";
        private static readonly DateTime Now = new(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);

        // Sesión Activa con un equipo que tiene un jugador registrado.
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

        // --- RequestHint ---

        [Fact]
        public void RequestHint_WithValidData_ShouldCreatePendingRequest()
        {
            var (session, team) = CreateActiveSessionWithPlayer();
            var subStageId = Guid.NewGuid();

            var request = session.RequestHint(team.Id, subStageId, PlayerId, Now);

            request.Status.Should().Be(HintRequestStatus.Pendiente);
            request.TeamId.Should().Be(team.Id);
            request.SubStageId.Should().Be(subStageId);
            session.HintRequests.Should().ContainSingle(r => r.Id == request.Id);
        }

        [Fact]
        public void RequestHint_WhenSessionNotActive_ShouldThrowBusinessException()
        {
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            session.StartPreparation();
            var team = Team.Create(Guid.NewGuid(), "Alfa", session.Id);
            session.RegisterTeam(team);
            session.AddPlayerToTeam(team.Id, Player.Create(Guid.NewGuid(), PlayerId, "Ana", team.Id));

            Action act = () => session.RequestHint(team.Id, Guid.NewGuid(), PlayerId, Now);

            act.Should().Throw<BusinessException>()
                .WithMessage("Solo se pueden solicitar pistas si la partida está activa.");
        }

        [Fact]
        public void RequestHint_WhenTeamNotInSession_ShouldThrowBusinessException()
        {
            var (session, _) = CreateActiveSessionWithPlayer();

            Action act = () => session.RequestHint(Guid.NewGuid(), Guid.NewGuid(), PlayerId, Now);

            act.Should().Throw<BusinessException>()
                .WithMessage("El equipo especificado no pertenece a esta sesión.");
        }

        [Fact]
        public void RequestHint_WhenPlayerNotInTeam_ShouldThrowBusinessException()
        {
            var (session, team) = CreateActiveSessionWithPlayer();

            Action act = () => session.RequestHint(team.Id, Guid.NewGuid(), "otro-usuario", Now);

            act.Should().Throw<BusinessException>()
                .WithMessage("El jugador no pertenece al equipo indicado.");
        }

        [Fact]
        public void RequestHint_WhenAlreadyPendingForSameSubStage_ShouldThrowBusinessException()
        {
            var (session, team) = CreateActiveSessionWithPlayer();
            var subStageId = Guid.NewGuid();
            session.RequestHint(team.Id, subStageId, PlayerId, Now);

            Action act = () => session.RequestHint(team.Id, subStageId, PlayerId, Now);

            act.Should().Throw<BusinessException>()
                .WithMessage("Ya tienes una solicitud de pista pendiente para este reto.");
        }

        // --- GrantHint ---

        [Fact]
        public void GrantHint_WithValidData_ShouldCreateReleasedHint()
        {
            var (session, team) = CreateActiveSessionWithPlayer();
            var subStageId = Guid.NewGuid();
            var clueId = Guid.NewGuid();

            var released = session.GrantHint(team.Id, subStageId, clueId, "Busca bajo la mesa", 1, Now);

            released.ClueId.Should().Be(clueId);
            released.ClueText.Should().Be("Busca bajo la mesa");
            released.TeamId.Should().Be(team.Id);
            session.ReleasedHints.Should().ContainSingle(h => h.Id == released.Id);
        }

        [Fact]
        public void GrantHint_ShouldResolvePendingRequestForSameTeamAndSubStage()
        {
            var (session, team) = CreateActiveSessionWithPlayer();
            var subStageId = Guid.NewGuid();
            var request = session.RequestHint(team.Id, subStageId, PlayerId, Now);

            session.GrantHint(team.Id, subStageId, Guid.NewGuid(), "Pista", 1, Now);

            request.Status.Should().Be(HintRequestStatus.Resuelta);
            request.ResolvedAt.Should().Be(Now);
        }

        [Fact]
        public void GrantHint_WhenSessionNotActive_ShouldThrowBusinessException()
        {
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            session.StartPreparation();
            var team = Team.Create(Guid.NewGuid(), "Alfa", session.Id);
            session.RegisterTeam(team);

            Action act = () => session.GrantHint(team.Id, Guid.NewGuid(), Guid.NewGuid(), "Pista", 1, Now);

            act.Should().Throw<BusinessException>()
                .WithMessage("Solo se pueden liberar pistas si la partida está activa.");
        }

        [Fact]
        public void GrantHint_WhenTeamNotInSession_ShouldThrowBusinessException()
        {
            var (session, _) = CreateActiveSessionWithPlayer();

            Action act = () => session.GrantHint(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Pista", 1, Now);

            act.Should().Throw<BusinessException>()
                .WithMessage("El equipo especificado no pertenece a esta sesión.");
        }

        [Fact]
        public void GrantHint_WhenSameClueAlreadyReleasedToTeam_ShouldThrowBusinessException_RB04()
        {
            var (session, team) = CreateActiveSessionWithPlayer();
            var subStageId = Guid.NewGuid();
            var clueId = Guid.NewGuid();
            session.GrantHint(team.Id, subStageId, clueId, "Pista", 1, Now);

            Action act = () => session.GrantHint(team.Id, subStageId, clueId, "Pista", 1, Now);

            act.Should().Throw<BusinessException>()
                .WithMessage("Esta pista ya fue liberada a este equipo.");
        }

        [Fact]
        public void GrantHint_SameClueToDifferentTeams_ShouldBeAllowed()
        {
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            session.StartPreparation();
            var teamA = Team.Create(Guid.NewGuid(), "Alfa", session.Id);
            var teamB = Team.Create(Guid.NewGuid(), "Beta", session.Id);
            session.RegisterTeam(teamA);
            session.RegisterTeam(teamB);
            session.Activate();

            var subStageId = Guid.NewGuid();
            var clueId = Guid.NewGuid();

            session.GrantHint(teamA.Id, subStageId, clueId, "Pista", 1, Now);
            Action act = () => session.GrantHint(teamB.Id, subStageId, clueId, "Pista", 1, Now);

            act.Should().NotThrow();
            session.ReleasedHints.Should().HaveCount(2);
        }
    }
}
