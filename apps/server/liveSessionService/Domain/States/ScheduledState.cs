using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Domain.States
{
    public class ScheduledState : LiveSessionState
    {
        public override LiveSessionStatus Status => LiveSessionStatus.Programada;

        public override void StartPreparation(LiveSession session)
        {
            session.TransitionTo(new PreparationState());
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
