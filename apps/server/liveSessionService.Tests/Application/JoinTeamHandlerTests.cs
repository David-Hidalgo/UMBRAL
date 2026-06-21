using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Commands;
using LiveSessionService.Application.Handlers;
using LiveSessionService.Application.IntegrationEvents;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.Repositories;

namespace LiveSessionService.Tests.Application
{
    public class JoinTeamHandlerTests
    {
        private readonly Mock<ILiveSessionRepository> _repositoryMock = new();
        private readonly Mock<IEventBus> _eventBusMock = new();
        private readonly Mock<ISessionNotificationService> _notificationMock = new();
        private readonly JoinTeamHandler _handler;

        public JoinTeamHandlerTests()
        {
            _handler = new JoinTeamHandler(
                _repositoryMock.Object,
                _eventBusMock.Object,
                _notificationMock.Object);
        }

        [Fact]
        public async Task Handle_WhenSessionDoesNotExist_ShouldThrowBusinessException()
        {
            // Arrange
            _repositoryMock
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((LiveSession?)null);

            var command = new JoinTeamCommand(Guid.NewGuid(), Guid.NewGuid(), "user-1", "Juan");

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<BusinessException>()
                .WithMessage("La sesión de juego especificada no existe.");

            _repositoryMock.Verify(r => r.AddPlayerAsync(It.IsAny<Player>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenTeamDoesNotExist_ShouldThrowBusinessException()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var command = new JoinTeamCommand(session.Id, Guid.NewGuid(), "user-1", "Juan");

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<BusinessException>()
                .WithMessage("El equipo especificado no pertenece a esta sesión.");
        }

        [Fact]
        public async Task Handle_WithValidRequest_ShouldPersistNotifyAndPublishEvent()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            var team = Team.Create(Guid.NewGuid(), "Alfa", session.Id);
            session.RegisterTeam(team);

            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var command = new JoinTeamCommand(session.Id, team.Id, "user-123", "Juan");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            team.Players.Should().ContainSingle(p => p.KeycloakUserId == "user-123" && p.DisplayName == "Juan");

            _repositoryMock.Verify(r => r.AddPlayerAsync(It.IsAny<Player>(), It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            _eventBusMock.Verify(
                e => e.PublishAsync(
                    It.Is<PlayerJoinedTeamIntegrationEvent>(ev =>
                        ev.SessionId == session.Id &&
                        ev.TeamId == team.Id &&
                        ev.KeycloakUserId == "user-123"),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _notificationMock.Verify(
                n => n.NotifyPlayerJoinedAsync(session.Id, team.Id, "Juan", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithDuplicatePlayer_ShouldThrowBusinessException()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            var team = Team.Create(Guid.NewGuid(), "Alfa", session.Id);
            session.RegisterTeam(team);
            session.AddPlayerToTeam(team.Id, Player.Create(Guid.NewGuid(), "user-123", "Juan", team.Id));

            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var command = new JoinTeamCommand(session.Id, team.Id, "user-123", "Juan Duplicado");

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<BusinessException>()
                .WithMessage("El jugador ya está registrado en esta sesión.");

            _repositoryMock.Verify(r => r.AddPlayerAsync(It.IsAny<Player>(), It.IsAny<CancellationToken>()), Times.Never);
            _notificationMock.Verify(
                n => n.NotifyPlayerJoinedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
