using System;
using System.Collections.Generic;
using ScoringService.Domain.Enums;

namespace ScoringService.Application.DTOs
{
    /// <summary>
    /// Proyección ligera del contenido de una etapa, obtenida de missionService, usada para
    /// autovalidar una evidencia. Para Trivia se comparan las respuestas correctas; para
    /// Búsqueda del Tesoro se compara el código secreto (ya normalizado en el origen).
    /// <see cref="Difficulty"/> es la dificultad de la misión, que determina el multiplicador
    /// de puntaje (patrón Strategy).
    /// </summary>
    public sealed record SubStageContent(
        Guid SubStageId,
        SubmissionType Type,
        int Points,
        IReadOnlyList<Guid> CorrectAnswerIds,
        string? SecretCode,
        MissionDifficulty Difficulty = MissionDifficulty.Beginner);
}
