using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Exceptions;

namespace LiveSessionService.Domain.States
{
    public class PreparationState : LiveSessionState
    {
        public override LiveSessionStatus Status => LiveSessionStatus.Preparacion;

        public override void Activate(LiveSession session)
        {
            if (session.Teams.Count < 1)
            {
                throw new BusinessException("Una sesión no puede iniciar si no posee al menos un equipo registrado.");
            }
            session.TransitionTo(new ActiveState());
        }

        public override void Cancel(LiveSession session)
        {
            session.TransitionTo(new CancelledState());
        }

        public override void RegisterTeam(LiveSession session, Team team)
        {
            session.AddTeamInternal(team);
        }
    }
}
