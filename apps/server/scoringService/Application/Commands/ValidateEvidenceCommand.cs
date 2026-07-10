using System;
using MediatR;
using ScoringService.Application.DTOs;

namespace ScoringService.Application.Commands
{
    /// <summary>
    /// El operador valida manualmente una evidencia que quedó pendiente (RF-10). Si la declara
    /// correcta se otorga el puntaje base de la etapa (obtenido de missionService).
    /// </summary>
    public sealed record ValidateEvidenceCommand(
        Guid SessionId,
        Guid SubmissionId,
        bool IsCorrect,
        string? Actor,
        string JwtToken) : IRequest<SubmitEvidenceResultDto>;
}
