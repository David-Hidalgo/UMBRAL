using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ScoringService.Domain.Entities;

namespace ScoringService.Domain.Repositories
{
    /// <summary>
    /// Puerto de persistencia del núcleo de puntuación (evidencias, libro mayor y auditoría).
    /// La implementación vive en Infrastructure; el dominio solo conoce esta interfaz.
    /// </summary>
    public interface IScoringRepository
    {
        Task AddSubmissionAsync(EvidenceSubmission submission, CancellationToken cancellationToken = default);
        Task<EvidenceSubmission?> GetSubmissionByIdAsync(Guid submissionId, CancellationToken cancellationToken = default);

        /// <summary>Indica si el equipo ya resolvió correctamente esta etapa (anti doble-puntaje).</summary>
        Task<bool> HasSolvedSubStageAsync(Guid sessionId, Guid teamId, Guid subStageId, CancellationToken cancellationToken = default);

        /// <summary>Indica si el equipo ya envió algún intento (correcto o no) a esta etapa. Se usa para
        /// la Trivia, que es de un solo intento.</summary>
        Task<bool> HasAnySubmissionAsync(Guid sessionId, Guid teamId, Guid subStageId, CancellationToken cancellationToken = default);

        /// <summary>Trae en una sola consulta todos los intentos de la sesión para las subetapas indicadas
        /// (proyección ligera). Evita el patrón N+1 al calcular el progreso de varios equipos/retos.</summary>
        Task<IReadOnlyList<SubStageSubmissionInfo>> GetSubmissionsForSubStagesAsync(
            Guid sessionId, IReadOnlyCollection<Guid> subStageIds, CancellationToken cancellationToken = default);

        Task AddScoreEntryAsync(ScoreEntry entry, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ScoreEntry>> GetScoreEntriesBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ScoreEntry>> GetScoreEntriesByTeamAsync(Guid sessionId, Guid teamId, CancellationToken cancellationToken = default);

        Task AddSessionEventAsync(SessionEvent sessionEvent, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SessionEvent>> GetSessionEventsAsync(Guid sessionId, CancellationToken cancellationToken = default);

        // Atomicidad de la operación CQRS: el Handler decide cuándo persistir (skill efcore-postgres §3).
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
