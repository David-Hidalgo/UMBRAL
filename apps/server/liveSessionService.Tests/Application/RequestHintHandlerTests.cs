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
    public class RequestHintHandlerTests
    {
        private const string PlayerId = "user-123";
        private readonly Mock<ILiveSessionRepository> _repositoryMock = new();
        private readonly Mock<IEventBus> _eventBusMock = new();
        private readonly Mock<ISessionNotificationService> _notificationMock = new();
        private readonly RequestHintHandler _handler;

        public RequestHintHandlerTests()
        {
            _handler = new RequestHintHandler(_repositoryMock.Object, _eventBusMock.Object, _notificationMock.Object);
        }

        [Fact]
        public async Task Handle_WhenSessionDoesNotExist_ShouldThrowBusinessException()
        {
            _repositoryMock
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((LiveSession?)null);

            var command = new RequestHintCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), PlayerId);

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<BusinessException>()
                .WithMessage("La sesión de juego especificada no existe.");
        }

        [Fact]
        public async Task Handle_WithValidRequest_ShouldPersistPublishAndNotifyOperator()
        {
            var (session, team) = CreateActiveSessionWithPlayer();
            var subStageId = Guid.NewGuid();

            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var command = new RequestHintCommand(session.Id, team.Id, subStageId, PlayerId);

            var requestId = await _handler.Handle(command, CancellationToken.None);

            requestId.Should().NotBeEmpty();
            session.HintRequests.Should().ContainSingle(r => r.Id == requestId);

            _repositoryMock.Verify(r => r.AddHintRequestAsync(It.Is<HintRequest>(h => h.TeamId == team.Id), It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _eventBusMock.Verify(
                e => e.PublishAsync(
                    It.Is<HintRequestedIntegrationEvent>(ev => ev.SessionId == session.Id && ev.TeamId == team.Id && ev.SubStageId == subStageId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            _notificationMock.Verify(
                n => n.NotifyHintRequestedAsync(session.Id, requestId, team.Id, "Alfa", subStageId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WhenPlayerNotInTeam_ShouldThrowAndNotPersist()
        {
            var (session, team) = CreateActiveSessionWithPlayer();

            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var command = new RequestHintCommand(session.Id, team.Id, Guid.NewGuid(), "otro-usuario");

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<BusinessException>()
                .WithMessage("El jugador no pertenece al equipo indicado.");

            _repositoryMock.Verify(r => r.AddHintRequestAsync(It.IsAny<HintRequest>(), It.IsAny<CancellationToken>()), Times.Never);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
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
