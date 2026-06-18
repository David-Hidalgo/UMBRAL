using System;
using System.Collections.Generic;
using System.Linq;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.States;

namespace LiveSessionService.Domain.Entities
{
    public class LiveSession
    {
        private readonly List<Team> _teams = new();

        public Guid Id { get; private set; }
        public Guid MissionId { get; private set; }
        public string MissionName { get; private set; } = null!;
        public string Name { get; private set; } = null!;
        public DateTime? ScheduledAt { get; private set; }
        public int CurrentStageIndex { get; private set; }
        public LiveSessionStatus Status { get; private set; }
        public IReadOnlyCollection<Team> Teams => _teams.AsReadOnly();

        public LiveSessionState State => GetStateInstance(Status);

        // Requerido por EF Core
        private LiveSession() { }

        public static LiveSession Create(Guid id, Guid missionId, string missionName, string name, DateTime? scheduledAt)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("El ID de la sesión no puede estar vacío.", nameof(id));

            if (missionId == Guid.Empty)
                throw new ArgumentException("El ID de la misión no puede estar vacío.", nameof(missionId));

            if (string.IsNullOrWhiteSpace(missionName))
                throw new ArgumentException("El nombre de la misión es requerido.", nameof(missionName));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("El nombre de la sesión es requerido.", nameof(name));

            return new LiveSession
            {
                Id = id,
                MissionId = missionId,
                MissionName = missionName,
                Name = name,
                ScheduledAt = scheduledAt,
                CurrentStageIndex = 0,
                Status = LiveSessionStatus.Programada
            };
        }

        public void StartPreparation()
        {
            State.StartPreparation(this);
        }

        public void Activate()
        {
            State.Activate(this);
        }

        public void Pause()
        {
            State.Pause(this);
        }

        public void Resume()
        {
            State.Resume(this);
        }

        public void Finish()
        {
            State.Finish(this);
        }

        public void Cancel()
        {
            State.Cancel(this);
        }

        public void RegisterTeam(Team team)
        {
            State.RegisterTeam(this, team);
        }

        public void TransitionTo(LiveSessionState newState)
        {
            Status = newState.Status;
        }

        public void AddTeamInternal(Team team)
        {
            if (team == null)
                throw new ArgumentNullException(nameof(team));

            if (_teams.Any(t => t.Name.Equals(team.Name, StringComparison.OrdinalIgnoreCase)))
                throw new BusinessException($"Ya existe un equipo con el nombre '{team.Name}' en esta sesión.");

            _teams.Add(team);
        }

        public void AddPlayerToTeam(Guid teamId, Player player)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));

            if (Status != LiveSessionStatus.Programada && Status != LiveSessionStatus.Preparacion)
                throw new BusinessException($"No se pueden unir jugadores en el estado actual de la partida: {Status}.");

            // Validar que el jugador no esté ya en la partida (en ningún equipo)
            var playerExists = _teams.Any(t => t.Players.Any(p => p.KeycloakUserId == player.KeycloakUserId));
            if (playerExists)
                throw new BusinessException("El jugador ya está registrado en esta sesión.");

            var team = _teams.FirstOrDefault(t => t.Id == teamId);
            if (team == null)
                throw new BusinessException("El equipo especificado no pertenece a esta sesión.");

            team.AddPlayer(player);
        }

        public void AdvanceStage(int nextStageIndex)
        {
            if (Status != LiveSessionStatus.Activa)
                throw new BusinessException("Solo se puede avanzar de etapa si la partida está activa.");

            if (nextStageIndex <= CurrentStageIndex)
                throw new BusinessException("El índice de la nueva etapa debe ser mayor que el índice actual.");

            CurrentStageIndex = nextStageIndex;
        }

        private LiveSessionState GetStateInstance(LiveSessionStatus status)
        {
            return status switch
            {
                LiveSessionStatus.Programada => new ScheduledState(),
                LiveSessionStatus.Preparacion => new PreparationState(),
                LiveSessionStatus.Activa => new ActiveState(),
                LiveSessionStatus.Pausada => new PausedState(),
                LiveSessionStatus.Finalizada => new FinishedState(),
                LiveSessionStatus.Cancelada => new CancelledState(),
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
            };
        }
    }
}
