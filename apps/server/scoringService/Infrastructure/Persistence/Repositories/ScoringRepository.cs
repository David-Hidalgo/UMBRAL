using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ScoringService.Domain.Entities;
using ScoringService.Domain.Repositories;
using ScoringService.Infrastructure.Persistence.Contexts;

namespace ScoringService.Infrastructure.Persistence.Repositories
{
    public sealed class ScoringRepository : IScoringRepository
    {
        private readonly ScoringDbContext _context;

        public ScoringRepository(ScoringDbContext context)
        {
            _context = context;
        }

        public async Task AddSubmissionAsync(EvidenceSubmission submission, CancellationToken cancellationToken = default)
            => await _context.EvidenceSubmissions.AddAsync(submission, cancellationToken);

        public async Task<EvidenceSubmission?> GetSubmissionByIdAsync(Guid submissionId, CancellationToken cancellationToken = default)
            => await _context.EvidenceSubmissions.FirstOrDefaultAsync(s => s.Id == submissionId, cancellationToken);

        public async Task AddScoreEntryAsync(ScoreEntry entry, CancellationToken cancellationToken = default)
            => await _context.ScoreEntries.AddAsync(entry, cancellationToken);

        public async Task<IReadOnlyList<ScoreEntry>> GetScoreEntriesBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
            => await _context.ScoreEntries
                .AsNoTracking()
                .Where(e => e.SessionId == sessionId)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<ScoreEntry>> GetScoreEntriesByTeamAsync(Guid sessionId, Guid teamId, CancellationToken cancellationToken = default)
            => await _context.ScoreEntries
                .AsNoTracking()
                .Where(e => e.SessionId == sessionId && e.TeamId == teamId)
                .ToListAsync(cancellationToken);

        public async Task AddSessionEventAsync(SessionEvent sessionEvent, CancellationToken cancellationToken = default)
            => await _context.SessionEvents.AddAsync(sessionEvent, cancellationToken);

        public async Task<IReadOnlyList<SessionEvent>> GetSessionEventsAsync(Guid sessionId, CancellationToken cancellationToken = default)
            => await _context.SessionEvents
                .AsNoTracking()
                .Where(e => e.SessionId == sessionId)
                .OrderByDescending(e => e.OccurredAt)
                .ToListAsync(cancellationToken);

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => await _context.SaveChangesAsync(cancellationToken);
    }
}
