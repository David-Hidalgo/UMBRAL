using System;
using FluentAssertions;
using Xunit;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.States;

namespace LiveSessionService.Tests.Domain
{
    public class LiveSessionStateTests
    {
        [Fact]
        public void Create_ShouldInitializeInScheduledState()
        {
            // Arrange
            var id = Guid.NewGuid();
            var missionId = Guid.NewGuid();

            // Act
            var session = LiveSession.Create(id, missionId, "Misión de prueba", "Sesión 1", null);

            // Assert
            session.Status.Should().Be(LiveSessionStatus.Programada);
            session.State.Should().BeOfType<ScheduledState>();
        }

        [Fact]
        public void StartPreparation_FromScheduled_ShouldTransitionToPreparation()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);

            // Act
            session.StartPreparation();

            // Assert
            session.Status.Should().Be(LiveSessionStatus.Preparacion);
            session.State.Should().BeOfType<PreparationState>();
        }

        [Fact]
        public void Activate_FromPreparation_WithoutTeams_ShouldThrowBusinessException()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            session.StartPreparation();

            // Act & Assert
            Action act = () => session.Activate();
            act.Should().Throw<BusinessException>()
               .WithMessage("Una sesión no puede iniciar si no posee al menos un equipo registrado.");
        }

        [Fact]
        public void Activate_FromPreparation_WithAtLeastOneTeam_ShouldTransitionToActive()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            session.StartPreparation();
            var team = Team.Create(Guid.NewGuid(), "Equipo Alfa", session.Id);
            session.RegisterTeam(team);

            // Act
            session.Activate();

            // Assert
            session.Status.Should().Be(LiveSessionStatus.Activa);
            session.State.Should().BeOfType<ActiveState>();
        }

        [Fact]
        public void Pause_FromActive_ShouldTransitionToPaused()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            session.StartPreparation();
            session.RegisterTeam(Team.Create(Guid.NewGuid(), "Equipo", session.Id));
            session.Activate();

            // Act
            session.Pause();

            // Assert
            session.Status.Should().Be(LiveSessionStatus.Pausada);
            session.State.Should().BeOfType<PausedState>();
        }

        [Fact]
        public void Resume_FromPaused_ShouldTransitionToActive()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            session.StartPreparation();
            session.RegisterTeam(Team.Create(Guid.NewGuid(), "Equipo", session.Id));
            session.Activate();
            session.Pause();

            // Act
            session.Resume();

            // Assert
            session.Status.Should().Be(LiveSessionStatus.Activa);
            session.State.Should().BeOfType<ActiveState>();
        }

        [Fact]
        public void Finish_FromActive_ShouldTransitionToFinished()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            session.StartPreparation();
            session.RegisterTeam(Team.Create(Guid.NewGuid(), "Equipo", session.Id));
            session.Activate();

            // Act
            session.Finish();

            // Assert
            session.Status.Should().Be(LiveSessionStatus.Finalizada);
            session.State.Should().BeOfType<FinishedState>();
        }

        [Fact]
        public void Cancel_FromPreparation_ShouldTransitionToCancelled()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            session.StartPreparation();

            // Act
            session.Cancel();

            // Assert
            session.Status.Should().Be(LiveSessionStatus.Cancelada);
            session.State.Should().BeOfType<CancelledState>();
        }

        [Fact]
        public void Cancel_FromScheduled_ShouldTransitionToCancelled()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);

            // Act
            session.Cancel();

            // Assert
            session.Status.Should().Be(LiveSessionStatus.Cancelada);
            session.State.Should().BeOfType<CancelledState>();
        }

        [Fact]
        public void Cancel_FromActive_ShouldTransitionToCancelled()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            session.StartPreparation();
            session.RegisterTeam(Team.Create(Guid.NewGuid(), "Equipo", session.Id));
            session.Activate();

            // Act
            session.Cancel();

            // Assert
            session.Status.Should().Be(LiveSessionStatus.Cancelada);
            session.State.Should().BeOfType<CancelledState>();
        }

        [Fact]
        public void Cancel_FromPaused_ShouldTransitionToCancelled()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            session.StartPreparation();
            session.RegisterTeam(Team.Create(Guid.NewGuid(), "Equipo", session.Id));
            session.Activate();
            session.Pause();

            // Act
            session.Cancel();

            // Assert
            session.Status.Should().Be(LiveSessionStatus.Cancelada);
            session.State.Should().BeOfType<CancelledState>();
        }

        [Fact]
        public void Cancel_FromFinished_ShouldThrowBusinessException()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            session.StartPreparation();
            session.RegisterTeam(Team.Create(Guid.NewGuid(), "Equipo", session.Id));
            session.Activate();
            session.Finish();

            // Act & Assert — Finalizada es el único estado terminal que NO admite cancelación (RB-09)
            Action act = () => session.Cancel();
            act.Should().Throw<BusinessException>()
                .WithMessage($"No se puede cancelar la partida desde el estado {LiveSessionStatus.Finalizada}.");
        }

        [Fact]
        public void Finish_FromPaused_ShouldTransitionToFinished()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            session.StartPreparation();
            session.RegisterTeam(Team.Create(Guid.NewGuid(), "Equipo", session.Id));
            session.Activate();
            session.Pause();

            // Act
            session.Finish();

            // Assert
            session.Status.Should().Be(LiveSessionStatus.Finalizada);
            session.State.Should().BeOfType<FinishedState>();
        }

        [Fact]
        public void InvalidTransitions_ShouldThrowBusinessException()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);

            // Act & Assert (Intentar pausar antes de activar)
            Action act = () => session.Pause();
            act.Should().Throw<BusinessException>();
        }

        [Fact]
        public void Activate_FromScheduled_ShouldThrowBusinessException()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);

            // Act & Assert
            Action act = () => session.Activate();
            act.Should().Throw<BusinessException>()
                .WithMessage($"No se puede activar la partida desde el estado {LiveSessionStatus.Programada}.");
        }

        [Fact]
        public void RegisterTeam_FromActiveState_ShouldThrowBusinessException()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            session.StartPreparation();
            session.RegisterTeam(Team.Create(Guid.NewGuid(), "Equipo", session.Id));
            session.Activate();

            // Act & Assert
            Action act = () => session.RegisterTeam(Team.Create(Guid.NewGuid(), "Otro", session.Id));
            act.Should().Throw<BusinessException>()
                .WithMessage($"No se puede registrar equipos en el estado {LiveSessionStatus.Activa}.");
        }

        [Fact]
        public void Finish_FromScheduled_ShouldThrowBusinessException()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);

            // Act & Assert
            Action act = () => session.Finish();
            act.Should().Throw<BusinessException>()
                .WithMessage($"No se puede finalizar la partida desde el estado {LiveSessionStatus.Programada}.");
        }

        [Fact]
        public void Resume_FromScheduled_ShouldThrowBusinessException()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);

            // Act & Assert
            Action act = () => session.Resume();
            act.Should().Throw<BusinessException>()
                .WithMessage($"No se puede reanudar la partida desde el estado {LiveSessionStatus.Programada}.");
        }

        [Fact]
        public void StartPreparation_FromActive_ShouldThrowBusinessException()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            session.StartPreparation();
            session.RegisterTeam(Team.Create(Guid.NewGuid(), "Equipo", session.Id));
            session.Activate();

            // Act & Assert
            Action act = () => session.StartPreparation();
            act.Should().Throw<BusinessException>()
                .WithMessage($"No se puede iniciar preparación desde el estado {LiveSessionStatus.Activa}.");
        }

        [Fact]
        public void RegisterTeam_FromPaused_ShouldThrowBusinessException()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            session.StartPreparation();
            session.RegisterTeam(Team.Create(Guid.NewGuid(), "Equipo", session.Id));
            session.Activate();
            session.Pause();

            // Act & Assert
            Action act = () => session.RegisterTeam(Team.Create(Guid.NewGuid(), "Otro", session.Id));
            act.Should().Throw<BusinessException>()
                .WithMessage($"No se puede registrar equipos en el estado {LiveSessionStatus.Pausada}.");
        }

        [Fact]
        public void StartPreparation_FromFinished_ShouldThrowBusinessException()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            session.StartPreparation();
            session.RegisterTeam(Team.Create(Guid.NewGuid(), "Equipo", session.Id));
            session.Activate();
            session.Finish();

            // Act & Assert
            Action act = () => session.StartPreparation();
            act.Should().Throw<BusinessException>()
                .WithMessage($"No se puede iniciar preparación desde el estado {LiveSessionStatus.Finalizada}.");
        }

        [Fact]
        public void Cancel_FromCancelled_ShouldThrowBusinessException()
        {
            // Arrange
            var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Sesión", null);
            session.Cancel();

            // Act & Assert
            Action act = () => session.Cancel();
            act.Should().Throw<BusinessException>()
                .WithMessage($"No se puede cancelar la partida desde el estado {LiveSessionStatus.Cancelada}.");
        }
    }
}
