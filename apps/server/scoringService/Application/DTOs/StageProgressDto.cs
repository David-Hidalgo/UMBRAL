using System;
using System.Collections.Generic;

namespace ScoringService.Application.DTOs
{
    /// <summary>
    /// Progreso de los equipos en la etapa actual, para que el operador sepa cuándo avanzar
    /// (no ve los teléfonos). "Completado" = el equipo ya no puede enviar más a esa subetapa.
    /// </summary>
    public sealed record StageProgressDto(
        int SubStageCount,
        IReadOnlyList<TeamStageProgressDto> Teams,
        bool AllComplete);

    public sealed record TeamStageProgressDto(Guid TeamId, string TeamCode, int Completed);
}
