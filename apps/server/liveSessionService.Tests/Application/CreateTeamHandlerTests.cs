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
    public class CreateTeamHandlerTests
    {
        private readonly Mock<ILiveSessionRepository> _repositoryMock = new();
        private readonly Mock<IEventBus> _eventBusMock = new();
        private readonly CreateTeamHandler _handler;

        public CreateTeamHandlerTests()
        {
            _handler = new CreateTeamHandler(_repositoryMock.Object, _eventBusMock.Object);
        }

        [Fact]
        public async Task Handle_WhenSessionDoesNotExist_ShouldThrowBusinessException()
        {
            // Arrange
            _repositoryMock
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((LiveSession?)null);

            var command = new CreateTeamCommand(Guid.NewGuid(), "Equipo Alfa");

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<BusinessException>()
                .WithMessage("La sesión de juego especificada no existe.");

            _repositoryMock.Verify(r => r.AddTeamAsync(It.IsAny<Team>(), It.IsAny<CancellationToken>()), Times.Never);
            _eventBusMock.Verify(
                e => e.PublishAsync(It.IsAny<TeamRegisteredIntegrationEvent>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WithValidSession_ShouldCreateTeamPersistAndPublishEvent()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var command = new CreateTeamCommand(session.Id, "Equipo Alfa");

            // Act
            var teamId = await _handler.Handle(command, CancellationToken.None);

            // Assert
            teamId.Should().NotBeEmpty();
            session.Teams.Should().ContainSingle(t => t.Name == "Equipo Alfa");

            _repositoryMock.Verify(r => r.AddTeamAsync(It.Is<Team>(t => t.Name == "Equipo Alfa"), It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _eventBusMock.Verify(
                e => e.PublishAsync(
                    It.Is<TeamRegisteredIntegrationEvent>(ev => ev.SessionId == session.Id && ev.TeamName == "Equipo Alfa"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithDuplicateTeamName_ShouldThrowBusinessException()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            session.RegisterTeam(Team.Create(Guid.NewGuid(), "Equipo Alfa", session.Id));

            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var command = new CreateTeamCommand(session.Id, "equipo alfa");

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<BusinessException>()
                .WithMessage("Ya existe un equipo con el nombre 'equipo alfa' en esta sesión.");

            _repositoryMock.Verify(r => r.AddTeamAsync(It.IsAny<Team>(), It.IsAny<CancellationToken>()), Times.Never);
            _eventBusMock.Verify(
                e => e.PublishAsync(It.IsAny<TeamRegisteredIntegrationEvent>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
