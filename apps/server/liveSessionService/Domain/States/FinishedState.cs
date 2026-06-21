using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Domain.States
{
    public class FinishedState : LiveSessionState
    {
        public override LiveSessionStatus Status => LiveSessionStatus.Finalizada;
    }
}
