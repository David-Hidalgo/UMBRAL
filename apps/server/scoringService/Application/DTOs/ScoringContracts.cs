using System;
using ScoringService.Domain.Enums;

namespace ScoringService.Application.DTOs
{
    /// <summary>Cuerpo de la petición de envío de evidencia desde la app móvil (RF-08).</summary>
    public sealed record SubmitEvidenceRequest(SubmissionType Type, Guid SubStageId, string Payload);

    /// <summary>Cuerpo de la validación manual del operador sobre una evidencia pendiente (RF-10).</summary>
    public sealed record ValidateEvidenceRequest(bool IsCorrect);

    /// <summary>Cuerpo de la penalización aplicada por el operador (RF-11, RB-06).</summary>
    public sealed record ApplyPenaltyRequest(int Points, string Reason);

    /// <summary>Resultado del envío/validación de una evidencia devuelto al cliente.</summary>
    public sealed record SubmitEvidenceResultDto(
        Guid SubmissionId,
        string Status,
        int AwardedPoints,
        int TeamTotalPoints);
}
