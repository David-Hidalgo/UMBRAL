using System;
using FluentAssertions;
using Xunit;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Tests.Support;

namespace LiveSessionService.Tests.Domain
{
    public class LiveSessionDomainTests
    {
        [Fact]
        public void AddTeam_ShouldRegisterSuccessfully()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            var team = Team.Create(Guid.NewGuid(), "Alfa", session.Id);

            // Act
            session.RegisterTeam(team);

            // Assert
            session.Teams.Should().Contain(team);
        }

        [Fact]
        public void AddDuplicateTeam_ShouldThrowBusinessException()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            var team1 = Team.Create(Guid.NewGuid(), "Alfa", session.Id);
            var team2 = Team.Create(Guid.NewGuid(), "alfa", session.Id); // Case-insensitive

            session.RegisterTeam(team1);

            // Act & Assert
            Action act = () => session.RegisterTeam(team2);
            act.Should().Throw<BusinessException>().WithMessage("Ya existe un equipo con el nombre 'alfa' en esta sesión.");
        }

        [Fact]
        public void AddPlayerToTeam_ShouldRegisterSuccessfully()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            var team = Team.Create(Guid.NewGuid(), "Alfa", session.Id);
            session.RegisterTeam(team);

            var player = Player.Create(Guid.NewGuid(), "user-123", "Juan", team.Id);

            // Act
            session.AddPlayerToTeam(team.Id, player);

            // Assert
            team.Players.Should().Contain(player);
        }

        [Fact]
        public void AddDuplicatePlayerInSession_ShouldThrowBusinessException()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            var team1 = Team.Create(Guid.NewGuid(), "Alfa", session.Id);
            var team2 = Team.Create(Guid.NewGuid(), "Beta", session.Id);
            session.RegisterTeam(team1);
            session.RegisterTeam(team2);

            var player1 = Player.Create(Guid.NewGuid(), "user-123", "Juan", team1.Id);
            var player2 = Player.Create(Guid.NewGuid(), "user-123", "Juan en Beta", team2.Id);

            session.AddPlayerToTeam(team1.Id, player1);

            // Act & Assert
            Action act = () => session.AddPlayerToTeam(team2.Id, player2);
            act.Should().Throw<BusinessException>().WithMessage("El jugador ya está registrado en esta sesión.");
        }

        [Fact]
        public void AddPlayerWhenSessionActive_ShouldThrowBusinessException()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            session.StartPreparation();
            var team = session.RegisterTeamWithPlayer("Alfa");
            session.Activate();

            var player = Player.Create(Guid.NewGuid(), "user-123", "Juan", team.Id);

            // Act & Assert
            Action act = () => session.AddPlayerToTeam(team.Id, player);
            act.Should().Throw<BusinessException>();
        }

        [Fact]
        public void AdvanceStage_WithValidIndex_ShouldUpdateCurrentStageIndex()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            session.StartPreparation();
            session.RegisterTeamWithPlayer();
            session.Activate();

            // Act
            session.AdvanceStage(2);

            // Assert
            session.CurrentStageIndex.Should().Be(2);
        }

        [Fact]
        public void AdvanceStage_WhenIndexNotGreaterThanCurrent_ShouldThrowBusinessException()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            session.StartPreparation();
            session.RegisterTeamWithPlayer();
            session.Activate();

            // Act & Assert
            Action act = () => session.AdvanceStage(0);
            act.Should().Throw<BusinessException>()
                .WithMessage("El índice de la nueva etapa debe ser mayor que el índice actual.");
        }

        [Fact]
        public void AdvanceStage_WhenSessionNotActive_ShouldThrowBusinessException()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);

            // Act & Assert
            Action act = () => session.AdvanceStage(1);
            act.Should().Throw<BusinessException>()
                .WithMessage("Solo se puede avanzar de etapa si la partida está activa.");
        }

        [Fact]
        public void Create_WithEmptyId_ShouldThrowArgumentException()
        {
            Action act = () => LiveSession.Create(Guid.Empty, Guid.NewGuid(), "Misión", "Sesión", null);
            act.Should().Throw<ArgumentException>().WithParameterName("id");
        }

        [Fact]
        public void Create_WithEmptyMissionId_ShouldThrowArgumentException()
        {
            Action act = () => LiveSession.Create(Guid.NewGuid(), Guid.Empty, "Misión", "Sesión", null);
            act.Should().Throw<ArgumentException>().WithParameterName("missionId");
        }

        [Fact]
        public void Create_WithNullOrWhitespaceMissionName_ShouldThrowArgumentException()
        {
            Action act1 = () => LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "", "Sesión", null);
            Action act2 = () => LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "   ", "Sesión", null);

            act1.Should().Throw<ArgumentException>().WithParameterName("missionName");
            act2.Should().Throw<ArgumentException>().WithParameterName("missionName");
        }

        [Fact]
        public void Create_WithNullOrWhitespaceName_ShouldThrowArgumentException()
        {
            Action act1 = () => LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "", null);
            Action act2 = () => LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "   ", null);

            act1.Should().Throw<ArgumentException>().WithParameterName("name");
            act2.Should().Throw<ArgumentException>().WithParameterName("name");
        }

        [Fact]
        public void AddTeamInternal_WithNullTeam_ShouldThrowArgumentNullException()
        {
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            Action act = () => session.AddTeamInternal(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("team");
        }

        [Fact]
        public void AddPlayerToTeam_WithNullPlayer_ShouldThrowArgumentNullException()
        {
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            var team = Team.Create(Guid.NewGuid(), "Alfa", session.Id);
            session.RegisterTeam(team);

            Action act = () => session.AddPlayerToTeam(team.Id, null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("player");
        }

        [Fact]
        public void AddPlayerToTeam_WithNonExistentTeamId_ShouldThrowBusinessException()
        {
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            var team = Team.Create(Guid.NewGuid(), "Alfa", session.Id);
            session.RegisterTeam(team);

            var player = Player.Create(Guid.NewGuid(), "user-123", "Ana", team.Id);

            Action act = () => session.AddPlayerToTeam(Guid.NewGuid(), player);
            act.Should().Throw<BusinessException>().WithMessage("El equipo especificado no pertenece a esta sesión.");
        }
    }
}
