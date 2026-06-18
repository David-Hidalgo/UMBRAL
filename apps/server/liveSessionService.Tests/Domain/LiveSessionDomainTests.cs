using System;
using FluentAssertions;
using Xunit;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Exceptions;

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
            var team = Team.Create(Guid.NewGuid(), "Alfa", session.Id);
            session.RegisterTeam(team);
            session.StartPreparation();
            session.Activate();

            var player = Player.Create(Guid.NewGuid(), "user-123", "Juan", team.Id);

            // Act & Assert
            Action act = () => session.AddPlayerToTeam(team.Id, player);
            act.Should().Throw<BusinessException>();
        }
    }
}
