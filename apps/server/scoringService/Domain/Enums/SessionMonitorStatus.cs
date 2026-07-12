namespace ScoringService.Domain.Enums
{
    /// <summary>
    /// Estado local espejeado de la sesión en vivo, alimentado por los eventos de RabbitMQ
    /// publicados por liveSessionService. Determina si se aceptan evidencias (RB-03).
    /// </summary>
    public enum SessionMonitorStatus
    {
        Pending = 0,
        Active = 1,
        Paused = 2,
        Finished = 3,
        Cancelled = 4
    }
}
