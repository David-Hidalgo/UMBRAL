using System;

namespace LiveSessionService.Application.IntegrationEvents
{
    // Publicado al liberar una pista. Deja la puerta abierta (OCP) a que el scoringService
    // lo consuma en el futuro (p. ej. penalizar puntos por pista) sin tocar este servicio.
    public record HintReleasedIntegrationEvent(Guid SessionId, Guid TeamId, Guid SubStageId, Guid ClueId, DateTime ReleasedAt);
}
