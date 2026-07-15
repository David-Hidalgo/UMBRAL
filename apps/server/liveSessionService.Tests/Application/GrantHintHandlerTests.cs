using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
    public class GrantHintHandlerTests
    {
        private const string PlayerId = "user-123";
        private readonly Mock<ILiveSessionRepository> _repositoryMock = new();
        private readonly Mock<IMissionServiceClient> _missionClientMock = new();
        private readonly Mock<IEventBus> _eventBusMock = new();
        private readonly Mock<ISessionNotificationService> _notificationMock = new();
        private readonly GrantHintHandler _handler;

        public GrantHintHandlerTests()
        {
            _handler = new GrantHintHandler(
                _repositoryMock.Object,
                _missionClientMock.Object,
                _eventBusMock.Object,
                _notificationMock.Object);
        }

        [Fact]
        public async Task Handle_WhenSessionDoesNotExist_ShouldThrowBusinessException()
        {
            _repositoryMock
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((LiveSession?)null);

            var command = new GrantHintCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "jwt");

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<BusinessException>()
                .WithMessage("La sesión de juego especificada no existe.");
        }

        [Fact]
        public async Task Handle_WhenMissionClientReturnsNull_ShouldThrowBusinessException()
        {
            var (session, team) = CreateActiveSessionWithPlayer();
            SetupSession(session);

            _missionClientMock
                .Setup(c => c.GetMissionByIdAsync(session.MissionId, "jwt", It.IsAny<CancellationToken>()))
                .ReturnsAsync((MissionClientDto?)null);

            var command = new GrantHintCommand(session.Id, team.Id, Guid.NewGuid(), Guid.NewGuid(), "jwt");

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<BusinessException>()
                .WithMessage("No se pudo obtener la estructura de la misión.");
        }

        [Fact]
        public async Task Handle_WhenSubStageNotInCurrentStage_ShouldThrowBusinessException()
        {
            var (session, team) = CreateActiveSessionWithPlayer();
            SetupSession(session);

            // La misión tiene una subetapa distinta a la solicitada.
            var mission = CreateMission(session.MissionId, Guid.NewGuid(), clueId: Guid.NewGuid());
            _missionClientMock
                .Setup(c => c.GetMissionByIdAsync(session.MissionId, "jwt", It.IsAny<CancellationToken>()))
                .ReturnsAsync(mission);

            var command = new GrantHintCommand(session.Id, team.Id, Guid.NewGuid(), Guid.NewGuid(), "jwt");

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<BusinessException>()
                .WithMessage("La subetapa indicada no pertenece a la etapa actual de la partida.");
        }

        [Fact]
        public async Task Handle_WhenSubStageHasNoClue_ShouldThrowBusinessException()
        {
            // Caso clave: la subetapa existe pero NO tiene la pista pedida (colección vacía).
            var (session, team) = CreateActiveSessionWithPlayer();
            SetupSession(session);

            var subStageId = Guid.NewGuid();
            var missionNoClues = new MissionClientDto(
                session.MissionId, "Misión", "Active", 60,
                new List<StageClientDto>
                {
                    new(Guid.NewGuid(), "Etapa 1", "Desc", 1, new List<SubStageClientDto>
                    {
                        new(subStageId, "Reto", 0, 100, new List<ClueClientDto>())
                    })
                });

            _missionClientMock
                .Setup(c => c.GetMissionByIdAsync(session.MissionId, "jwt", It.IsAny<CancellationToken>()))
                .ReturnsAsync(missionNoClues);

            var command = new GrantHintCommand(session.Id, team.Id, subStageId, Guid.NewGuid(), "jwt");

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<BusinessException>()
                .WithMessage("La pista indicada no existe para esta subetapa.");

            _repositoryMock.Verify(r => r.AddReleasedHintAsync(It.IsAny<ReleasedHint>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithValidClue_ShouldReleasePersistPublishAndNotifyTeam()
        {
            var (session, team) = CreateActiveSessionWithPlayer();
            SetupSession(session);

            var subStageId = Guid.NewGuid();
            var clueId = Guid.NewGuid();
            var mission = CreateMission(session.MissionId, subStageId, clueId);

            _missionClientMock
                .Setup(c => c.GetMissionByIdAsync(session.MissionId, "jwt", It.IsAny<CancellationToken>()))
                .ReturnsAsync(mission);

            var command = new GrantHintCommand(session.Id, team.Id, subStageId, clueId, "jwt");

            await _handler.Handle(command, CancellationToken.None);

            session.ReleasedHints.Should().ContainSingle(h => h.ClueId == clueId && h.TeamId == team.Id);

            _repositoryMock.Verify(r => r.AddReleasedHintAsync(It.Is<ReleasedHint>(h => h.ClueId == clueId), It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _eventBusMock.Verify(
                e => e.PublishAsync(
                    It.Is<HintReleasedIntegrationEvent>(ev => ev.TeamId == team.Id && ev.ClueId == clueId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            _notificationMock.Verify(
                n => n.NotifyHintReleasedAsync(session.Id, team.Id, It.IsAny<Guid>(), subStageId, clueId, "Pista secreta", 1, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
                Times.Once);
            _notificationMock.Verify(
                n => n.NotifyHintRequestResolvedAsync(session.Id, team.Id, subStageId, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WhenClueAlreadyReleasedToTeam_ShouldThrowBusinessException_RB04()
        {
            var (session, team) = CreateActiveSessionWithPlayer();
            var subStageId = Guid.NewGuid();
            var clueId = Guid.NewGuid();
            // Ya se le liberó esta pista al equipo previamente.
            session.GrantHint(team.Id, subStageId, clueId, "Pista secreta", 1, DateTime.UtcNow);
            SetupSession(session);

            var mission = CreateMission(session.MissionId, subStageId, clueId);
            _missionClientMock
                .Setup(c => c.GetMissionByIdAsync(session.MissionId, "jwt", It.IsAny<CancellationToken>()))
                .ReturnsAsync(mission);

            var command = new GrantHintCommand(session.Id, team.Id, subStageId, clueId, "jwt");

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<BusinessException>()
                .WithMessage("Esta pista ya fue liberada a este equipo.");
        }

        [Fact]
        public async Task Handle_WhenUniqueIndexViolatedOnSave_ShouldThrowBusinessException_RB04()
        {
            // Carrera concurrente: el chequeo en memoria pasa, pero el índice único de la BD
            // rechaza el duplicado. Debe traducirse a mensaje de negocio, no propagar un 500.
            var (session, team) = CreateActiveSessionWithPlayer();
            SetupSession(session);

            var subStageId = Guid.NewGuid();
            var clueId = Guid.NewGuid();
            var mission = CreateMission(session.MissionId, subStageId, clueId);
            _missionClientMock
                .Setup(c => c.GetMissionByIdAsync(session.MissionId, "jwt", It.IsAny<CancellationToken>()))
                .ReturnsAsync(mission);

            _repositoryMock
                .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("unique violation"));

            var command = new GrantHintCommand(session.Id, team.Id, subStageId, clueId, "jwt");

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<BusinessException>()
                .WithMessage("Esta pista ya fue liberada a este equipo.");

            // No debe publicar el evento ni notificar si la persistencia falló.
            _eventBusMock.Verify(
                e => e.PublishAsync(It.IsAny<HintReleasedIntegrationEvent>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _notificationMock.Verify(
                n => n.NotifyHintReleasedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private void SetupSession(LiveSession session)
        {
            _repositoryMock
                .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
        }

        private static MissionClientDto CreateMission(Guid missionId, Guid subStageId, Guid clueId)
        {
            return new MissionClientDto(
                missionId, "Misión", "Active", 60,
                new List<StageClientDto>
                {
                    new(Guid.NewGuid(), "Etapa 1", "Desc", 1, new List<SubStageClientDto>
                    {
                        new(subStageId, "Reto", 0, 100, new List<ClueClientDto>
                        {
                            new(clueId, "Pista secreta", 1)
                        })
                    })
                });
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
