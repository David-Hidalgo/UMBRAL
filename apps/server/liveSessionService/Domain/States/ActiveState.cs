using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Domain.States
{
    public class ActiveState : LiveSessionState
    {
        public override LiveSessionStatus Status => LiveSessionStatus.Activa;

        public override void Pause(LiveSession session)
        {
            session.TransitionTo(new PausedState());
        }

        public override void Finish(LiveSession session)
        {
            session.TransitionTo(new FinishedState());
        }

        public override void Cancel(LiveSession session)
        {
            session.TransitionTo(new CancelledState());
        }
    }
}
