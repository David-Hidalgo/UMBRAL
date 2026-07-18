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
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.Repositories;
using LiveSessionService.Domain.States;
using LiveSessionService.Tests.Support;

namespace LiveSessionService.Tests.Application
{
    public class TransitionSessionHandlerTests
    {
        private readonly Mock<ILiveSessionRepository> _repositoryMock = new();
        private readonly Mock<IMissionServiceClient> _missionClientMock = new();
        private readonly Mock<IEventBus> _eventBusMock = new();
        private readonly Mock<ISessionNotificationService> _notificationMock = new();
        private readonly TransitionSessionHandler _handler;

        public TransitionSessionHandlerTests()
        {
            _handler = new TransitionSessionHandler(
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

            var command = new TransitionSessionCommand(Guid.NewGuid(), LiveSessionStatus.Activa, "jwt");

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<BusinessException>()
                .WithMessage("La sesión de juego especificada no existe.");
        }

        [Fact]
        public async Task Handle_WhenAlreadyInTargetStatus_ShouldReturnWithoutSideEffects()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var command = new TransitionSessionCommand(session.Id, LiveSessionStatus.Programada, "jwt");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            _notificationMock.Verify(
                n => n.NotifyStateChangedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ActivateFromPreparation_ShouldPublishSessionActivatedAndNotify()
        {
            // Arrange
            var session = CreateSessionInPreparationWithTeam();
            var subStageId = Guid.NewGuid();
            SetupMissionClient(session.MissionId, subStageId);

            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var command = new TransitionSessionCommand(session.Id, LiveSessionStatus.Activa, "jwt");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            session.Status.Should().Be(LiveSessionStatus.Activa);
            session.State.Should().BeOfType<ActiveState>();

            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _eventBusMock.Verify(
                e => e.PublishAsync(
                    It.Is<SessionActivatedIntegrationEvent>(ev => ev.SubStageIds.Contains(subStageId)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            _notificationMock.Verify(
                n => n.NotifyStateChangedAsync(session.Id, LiveSessionStatus.Activa.ToString(), It.IsAny<DateTime?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_PauseFromActive_ShouldPublishSessionPausedAndNotify()
        {
            // Arrange
            var session = CreateActiveSession();
            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var command = new TransitionSessionCommand(session.Id, LiveSessionStatus.Pausada, "jwt");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            session.Status.Should().Be(LiveSessionStatus.Pausada);

            _eventBusMock.Verify(
                e => e.PublishAsync(It.IsAny<SessionPausedIntegrationEvent>(), It.IsAny<CancellationToken>()),
                Times.Once);
            _notificationMock.Verify(
                n => n.NotifyStateChangedAsync(session.Id, LiveSessionStatus.Pausada.ToString(), It.IsAny<DateTime?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ResumeFromPaused_ShouldNotPublishSessionActivated()
        {
            // Arrange
            var session = CreateActiveSession();
            session.Pause();

            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var command = new TransitionSessionCommand(session.Id, LiveSessionStatus.Activa, "jwt");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            session.Status.Should().Be(LiveSessionStatus.Activa);

            _eventBusMock.Verify(
                e => e.PublishAsync(It.IsAny<SessionActivatedIntegrationEvent>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _notificationMock.Verify(
                n => n.NotifyStateChangedAsync(session.Id, LiveSessionStatus.Activa.ToString(), It.IsAny<DateTime?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_FinishFromActive_ShouldPublishSessionFinishedAndNotify()
        {
            // Arrange
            var session = CreateActiveSession();
            var subStageId = Guid.NewGuid();
            SetupMissionClient(session.MissionId, subStageId);

            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var command = new TransitionSessionCommand(session.Id, LiveSessionStatus.Finalizada, "jwt");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            session.Status.Should().Be(LiveSessionStatus.Finalizada);

            _eventBusMock.Verify(
                e => e.PublishAsync(
                    It.Is<SessionFinishedIntegrationEvent>(ev => ev.SubStageIds.Contains(subStageId)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_CancelFromPreparation_ShouldPublishSessionCancelledWithEmptySubStages()
        {
            // Arrange
            var session = CreateSessionInPreparationWithTeam();
            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var command = new TransitionSessionCommand(session.Id, LiveSessionStatus.Cancelada, "jwt");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            session.Status.Should().Be(LiveSessionStatus.Cancelada);

            _eventBusMock.Verify(
                e => e.PublishAsync(
                    It.Is<SessionCancelledIntegrationEvent>(ev => ev.SubStageIds.Count == 0),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_CancelFromActive_ShouldTransitionToCancelledAndPublishEventWithSubStages()
        {
            // Arrange — cancelar desde Activa es un cierre forzado válido (RB-09 / HU-31);
            // al venir de Activa se decrementa el conteo de la etapa actual.
            var session = CreateActiveSession();
            var subStageId = Guid.NewGuid();
            SetupMissionClient(session.MissionId, subStageId);

            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var command = new TransitionSessionCommand(session.Id, LiveSessionStatus.Cancelada, "jwt");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            session.Status.Should().Be(LiveSessionStatus.Cancelada);
            session.State.Should().BeOfType<CancelledState>();

            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _eventBusMock.Verify(
                e => e.PublishAsync(
                    It.Is<SessionCancelledIntegrationEvent>(ev => ev.SubStageIds.Contains(subStageId)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            _notificationMock.Verify(
                n => n.NotifyStateChangedAsync(session.Id, LiveSessionStatus.Cancelada.ToString(), It.IsAny<DateTime?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_CancelFromPaused_ShouldTransitionToCancelledAndPublishEventWithSubStages()
        {
            // Arrange — cancelar desde Pausada también es válido y decrementa el conteo de la etapa actual.
            var session = CreateActiveSession();
            session.Pause();
            var subStageId = Guid.NewGuid();
            SetupMissionClient(session.MissionId, subStageId);

            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var command = new TransitionSessionCommand(session.Id, LiveSessionStatus.Cancelada, "jwt");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            session.Status.Should().Be(LiveSessionStatus.Cancelada);
            session.State.Should().BeOfType<CancelledState>();

            _eventBusMock.Verify(
                e => e.PublishAsync(
                    It.Is<SessionCancelledIntegrationEvent>(ev => ev.SubStageIds.Contains(subStageId)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_StartPreparationFromScheduled_ShouldTransitionAndNotify()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var command = new TransitionSessionCommand(session.Id, LiveSessionStatus.Preparacion, "jwt");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            session.Status.Should().Be(LiveSessionStatus.Preparacion);
            _notificationMock.Verify(
                n => n.NotifyStateChangedAsync(session.Id, LiveSessionStatus.Preparacion.ToString(), It.IsAny<DateTime?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ActivateFromScheduled_ShouldThrowBusinessException()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var command = new TransitionSessionCommand(session.Id, LiveSessionStatus.Activa, "jwt");

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<BusinessException>()
                .WithMessage($"No se puede activar la partida desde el estado {LiveSessionStatus.Programada}.");
        }

        [Fact]
        public async Task Handle_ActivateWhenMissionNotFound_ShouldThrowBusinessException()
        {
            // Arrange
            var session = CreateSessionInPreparationWithTeam();
            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            _missionClientMock
                .Setup(c => c.GetMissionByIdAsync(session.MissionId, "jwt", It.IsAny<CancellationToken>()))
                .ReturnsAsync((MissionClientDto?)null);

            var command = new TransitionSessionCommand(session.Id, LiveSessionStatus.Activa, "jwt");

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<BusinessException>()
                .WithMessage("No se pudo obtener la estructura de la misión.");
        }

        private static LiveSession CreateSessionInPreparationWithTeam()
        {
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            session.StartPreparation();
            session.RegisterTeamWithPlayer();
            return session;
        }

        private static LiveSession CreateActiveSession()
        {
            var session = CreateSessionInPreparationWithTeam();
            session.Activate();
            return session;
        }

        private void SetupMissionClient(Guid missionId, Guid subStageId)
        {
            _missionClientMock
                .Setup(c => c.GetMissionByIdAsync(missionId, "jwt", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MissionClientDto(
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
                            0,
                            new List<SubStageClientDto> { new(subStageId, "Sub 1", 0, 10) })
                    }
                ));
        }

        [Fact]
        public async Task Handle_WithUnsupportedTargetStatus_ShouldThrowBusinessException()
        {
            // Arrange
            var session = CreateSessionInPreparationWithTeam();
            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var command = new TransitionSessionCommand(session.Id, (LiveSessionStatus)99, "jwt");

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<BusinessException>()
                .WithMessage("Transición no soportada al estado: 99");
        }

        [Fact]
        public async Task Handle_WhenStageOrderNotMatchedButPhysicalIndexMatched_ShouldResolveStage()
        {
            // Arrange
            var session = CreateActiveSession();
            var subStageId = Guid.NewGuid();
            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            _missionClientMock
                .Setup(c => c.GetMissionByIdAsync(session.MissionId, "jwt", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MissionClientDto(
                    session.MissionId,
                    "Misión",
                    "Active",
                    60,
                    new List<StageClientDto>
                    {
                        new(
                            Guid.NewGuid(),
                            "Etapa 1",
                            "Desc",
                            5, 
                            new List<SubStageClientDto> { new(subStageId, "Sub 1", 0, 10) })
                    }));

            var command = new TransitionSessionCommand(session.Id, LiveSessionStatus.Finalizada, "jwt");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _eventBusMock.Verify(
                e => e.PublishAsync(
                    It.Is<SessionFinishedIntegrationEvent>(ev => ev.SubStageIds.Contains(subStageId)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WhenStageNotFoundByOrderOrPhysicalIndex_ShouldThrowBusinessException()
        {
            // Arrange
            var session = CreateActiveSession();
            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            _missionClientMock
                .Setup(c => c.GetMissionByIdAsync(session.MissionId, "jwt", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MissionClientDto(
                    session.MissionId,
                    "Misión",
                    "Active",
                    60,
                    new List<StageClientDto>())); 

            var command = new TransitionSessionCommand(session.Id, LiveSessionStatus.Finalizada, "jwt");

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<BusinessException>()
                .WithMessage($"No se encontró la etapa con índice {session.CurrentStageIndex} en la misión.");
        }
    }
}
