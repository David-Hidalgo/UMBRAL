using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Commands;
using LiveSessionService.Application.DTOs;
using LiveSessionService.Application.Handlers;
using LiveSessionService.Application.IntegrationEvents;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.Repositories;

namespace LiveSessionService.Tests.Application
{
    public class AdvanceStageHandlerTests
    {
        private readonly Mock<ILiveSessionRepository> _repositoryMock = new();
        private readonly Mock<IMissionServiceClient> _missionClientMock = new();
        private readonly Mock<IEventBus> _eventBusMock = new();
        private readonly Mock<ISessionNotificationService> _notificationMock = new();
        private readonly AdvanceStageHandler _handler;

        public AdvanceStageHandlerTests()
        {
            _handler = new AdvanceStageHandler(
                _repositoryMock.Object,
                _missionClientMock.Object,
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

            var command = new AdvanceStageCommand(Guid.NewGuid(), 1, "jwt");

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<BusinessException>()
                .WithMessage("La sesión de juego especificada no existe.");
        }

        [Fact]
        public async Task Handle_WithValidAdvance_ShouldSaveNotifyAndPublishEvent()
        {
            // Arrange
            var session = CreateActiveSession();
            var subStageId = Guid.NewGuid();
            var missionDto = CreateMissionWithStage(session.MissionId, 0, subStageId);

            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            _missionClientMock
                .Setup(c => c.GetMissionByIdAsync(session.MissionId, "jwt", It.IsAny<CancellationToken>()))
                .ReturnsAsync(missionDto);

            var command = new AdvanceStageCommand(session.Id, 1, "jwt");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            session.CurrentStageIndex.Should().Be(1);

            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _eventBusMock.Verify(
                e => e.PublishAsync(It.IsAny<StageTransitionedIntegrationEvent>(), It.IsAny<CancellationToken>()),
                Times.Once);
            _notificationMock.Verify(
                n => n.NotifyStageAdvancedAsync(session.Id, 1, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WhenMissionClientReturnsNull_ShouldPublishEventWithEmptySubStageLists()
        {
            // Arrange
            var session = CreateActiveSession();

            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            _missionClientMock
                .Setup(c => c.GetMissionByIdAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((MissionClientDto?)null);

            var command = new AdvanceStageCommand(session.Id, 1, "jwt");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _eventBusMock.Verify(
                e => e.PublishAsync(
                    It.Is<StageTransitionedIntegrationEvent>(ev =>
                        ev.PreviousSubStageIds.Count == 0 && ev.NewSubStageIds.Count == 0),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WhenMissionClientThrows_ShouldPublishEventWithEmptySubStageLists()
        {
            // Arrange
            var session = CreateActiveSession();

            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            _missionClientMock
                .Setup(c => c.GetMissionByIdAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("mission service unavailable"));

            var command = new AdvanceStageCommand(session.Id, 1, "jwt");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _eventBusMock.Verify(
                e => e.PublishAsync(
                    It.Is<StageTransitionedIntegrationEvent>(ev =>
                        ev.PreviousSubStageIds.Count == 0 && ev.NewSubStageIds.Count == 0),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldResolveSubStagesByListIndexWhenOrderDoesNotMatch()
        {
            // Arrange
            var session = CreateActiveSession();
            var subStageId = Guid.NewGuid();
            var missionDto = new MissionClientDto(
                session.MissionId,
                "Misión",
                "Active",
                60,
                new List<StageClientDto>
                {
                    new(Guid.NewGuid(), "Etapa 0", "Desc", 99, new List<SubStageClientDto>()),
                    new(Guid.NewGuid(), "Etapa 1", "Desc", 99, new List<SubStageClientDto>
                    {
                        new(subStageId, "Sub", 0, 10)
                    })
                });

            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            _missionClientMock
                .Setup(c => c.GetMissionByIdAsync(session.MissionId, "jwt", It.IsAny<CancellationToken>()))
                .ReturnsAsync(missionDto);

            var command = new AdvanceStageCommand(session.Id, 1, "jwt");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _eventBusMock.Verify(
                e => e.PublishAsync(
                    It.Is<StageTransitionedIntegrationEvent>(ev => ev.NewSubStageIds.Contains(subStageId)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WhenSessionNotActive_ShouldThrowBusinessException()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var command = new AdvanceStageCommand(session.Id, 1, "jwt");

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<BusinessException>()
                .WithMessage("Solo se puede avanzar de etapa si la partida está activa.");
        }

        private static LiveSession CreateActiveSession()
        {
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            session.StartPreparation();
            var team = Team.Create(Guid.NewGuid(), "Equipo", session.Id);
            session.RegisterTeam(team);
            session.Activate();
            return session;
        }

        private static MissionClientDto CreateMissionWithStage(Guid missionId, int stageOrder, Guid subStageId)
        {
            return new MissionClientDto(
                missionId,
                "Misión",
                "Active",
                60,
                new List<StageClientDto>
                {
                    new(
                        Guid.NewGuid(),
                        "Etapa 1",
                        "Desc",
                        stageOrder,
                        new List<SubStageClientDto>
                        {
                            new(subStageId, "Sub 1", 0, 10)
                        })
                });
        }
    }
}
