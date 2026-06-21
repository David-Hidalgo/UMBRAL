using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Exceptions;

namespace LiveSessionService.Domain.States
{
    public abstract class LiveSessionState
    {
        public abstract LiveSessionStatus Status { get; }

        public virtual void StartPreparation(LiveSession session) => 
            throw new BusinessException($"No se puede iniciar preparación desde el estado {Status}.");

        public virtual void Activate(LiveSession session) => 
            throw new BusinessException($"No se puede activar la partida desde el estado {Status}.");

        public virtual void Pause(LiveSession session) => 
            throw new BusinessException($"No se puede pausar la partida desde el estado {Status}.");

        public virtual void Resume(LiveSession session) => 
            throw new BusinessException($"No se puede reanudar la partida desde el estado {Status}.");

        public virtual void Finish(LiveSession session) => 
            throw new BusinessException($"No se puede finalizar la partida desde el estado {Status}.");

        public virtual void Cancel(LiveSession session) => 
            throw new BusinessException($"No se puede cancelar la partida desde el estado {Status}.");

        public virtual void RegisterTeam(LiveSession session, Team team) => 
            throw new BusinessException($"No se puede registrar equipos en el estado {Status}.");
    }
}
