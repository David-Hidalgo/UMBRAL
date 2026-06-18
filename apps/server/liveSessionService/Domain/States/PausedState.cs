using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Domain.States
{
    public class PausedState : LiveSessionState
    {
        public override LiveSessionStatus Status => LiveSessionStatus.Pausada;

        public override void Resume(LiveSession session)
        {
            session.TransitionTo(new ActiveState());
        }

        public override void Finish(LiveSession session)
        {
            session.TransitionTo(new FinishedState());
        }
    }
}
