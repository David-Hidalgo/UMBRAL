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

        Task AddScoreEntryAsync(ScoreEntry entry, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ScoreEntry>> GetScoreEntriesBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ScoreEntry>> GetScoreEntriesByTeamAsync(Guid sessionId, Guid teamId, CancellationToken cancellationToken = default);

        Task AddSessionEventAsync(SessionEvent sessionEvent, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SessionEvent>> GetSessionEventsAsync(Guid sessionId, CancellationToken cancellationToken = default);

        // Atomicidad de la operación CQRS: el Handler decide cuándo persistir (skill efcore-postgres §3).
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
