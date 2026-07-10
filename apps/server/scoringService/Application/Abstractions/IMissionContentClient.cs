using System;
using System.Threading;
using System.Threading.Tasks;
using ScoringService.Application.DTOs;

namespace ScoringService.Application.Abstractions
{
    /// <summary>
    /// Puerto hacia missionService (adaptador HTTP). Obtiene el contenido de una etapa
    /// (puntaje, respuestas correctas, código secreto) necesario para validar una evidencia.
    /// </summary>
    public interface IMissionContentClient
    {
        Task<SubStageContent?> GetSubStageContentAsync(
            Guid missionId, Guid subStageId, string jwtToken, CancellationToken cancellationToken = default);
    }
}
