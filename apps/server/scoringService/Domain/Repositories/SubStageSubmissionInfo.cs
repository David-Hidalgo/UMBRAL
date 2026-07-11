using System;
using ScoringService.Domain.Enums;

namespace ScoringService.Domain.Repositories
{
    /// <summary>
    /// Proyección ligera de un intento de un equipo sobre una subetapa. Se usa para calcular en
    /// memoria qué subetapas ha "cerrado" cada equipo sin lanzar una consulta por (equipo × subetapa).
    /// </summary>
    public sealed record SubStageSubmissionInfo(Guid TeamId, Guid SubStageId, SubmissionStatus Status);
}
