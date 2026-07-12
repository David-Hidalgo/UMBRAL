using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ScoringService.Application.DTOs;

namespace ScoringService.Application.Abstractions
{
    /// <summary>
    /// Puerto hacia missionService (adaptador HTTP). Obtiene el contenido de una etapa
    /// (puntaje, respuestas correctas, código secreto) necesario para validar una evidencia.
    /// Usa la identidad de servicio del scoring, no el token del jugador.
    /// </summary>
    public interface IMissionContentClient
    {
        Task<SubStageContent?> GetSubStageContentAsync(
            Guid missionId, Guid subStageId, CancellationToken cancellationToken = default);

        /// <summary>Proyección segura del reto para el jugador (sin respuestas correctas ni código secreto).</summary>
        Task<IReadOnlyList<PlayerSubStageDto>> GetPlayerSubStagesAsync(
            Guid missionId, IReadOnlyCollection<Guid> subStageIds, CancellationToken cancellationToken = default);
    }
}
