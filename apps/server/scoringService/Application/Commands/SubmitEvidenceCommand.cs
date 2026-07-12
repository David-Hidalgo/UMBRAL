using System;
using MediatR;
using ScoringService.Application.DTOs;
using ScoringService.Domain.Enums;

namespace ScoringService.Application.Commands
{
    /// <summary>
    /// Un equipo envía una respuesta/evidencia para una etapa (RF-08). El handler autovalida
    /// Trivia y Búsqueda del Tesoro contra el contenido de missionService y otorga el puntaje.
    /// </summary>
    public sealed record SubmitEvidenceCommand(
        Guid SessionId,
        Guid TeamId,
        Guid SubStageId,
        SubmissionType Type,
        string Payload) : IRequest<SubmitEvidenceResultDto>;
}
