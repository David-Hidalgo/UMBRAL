using Microsoft.EntityFrameworkCore;
using ScoringService.Domain.Entities;
using ScoringService.Infrastructure.Persistence.Inbox;

namespace ScoringService.Infrastructure.Persistence.Contexts
{
    public class ScoringDbContext : DbContext
    {
        public ScoringDbContext(DbContextOptions<ScoringDbContext> options) : base(options) { }

        public DbSet<EvidenceSubmission> EvidenceSubmissions => Set<EvidenceSubmission>();
        public DbSet<ScoreEntry> ScoreEntries => Set<ScoreEntry>();
        public DbSet<SessionEvent> SessionEvents => Set<SessionEvent>();
        public DbSet<SessionMonitor> SessionMonitors => Set<SessionMonitor>();
        public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Auto-descubre todas las IEntityTypeConfiguration<T> del ensamblado (skill efcore-postgres §1).
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ScoringDbContext).Assembly);
        }
    }
}
