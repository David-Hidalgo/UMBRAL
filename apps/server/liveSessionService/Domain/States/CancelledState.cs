using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Domain.States
{
    public class CancelledState : LiveSessionState
    {
        public override LiveSessionStatus Status => LiveSessionStatus.Cancelada;
    }
}
