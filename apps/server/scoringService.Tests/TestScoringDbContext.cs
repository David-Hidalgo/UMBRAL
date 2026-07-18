using System;
using Microsoft.EntityFrameworkCore;
using ScoringService.Domain.Entities;
using ScoringService.Domain.ValueObjects;
using ScoringService.Infrastructure.Persistence.Contexts;
using ScoringService.Infrastructure.Persistence.Configurations;

namespace ScoringService.Tests
{
    public class TestScoringDbContext : ScoringDbContext
    {
        public TestScoringDbContext(DbContextOptions<ScoringDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new EvidenceSubmissionConfiguration());
            modelBuilder.ApplyConfiguration(new ProcessedEventConfiguration());
            modelBuilder.ApplyConfiguration(new SessionEventConfiguration());
            modelBuilder.ApplyConfiguration(new SessionMonitorConfiguration());

            // ScoreEntry manual configuration to use OwnsOne instead of ComplexProperty for InMemory compatibility
            var builder = modelBuilder.Entity<ScoreEntry>();
            builder.ToTable("ScoreEntries");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.SessionId).IsRequired();
            builder.Property(x => x.TeamId).IsRequired();

            builder.OwnsOne(x => x.Value, v =>
            {
                v.Property(p => p.Points).HasColumnName("Points").IsRequired();
            });

            builder.Property(x => x.Origin).HasConversion<string>().HasMaxLength(30).IsRequired();
            builder.Property(x => x.SourceSubmissionId);

            builder.Property(x => x.Reason)
                .HasConversion(
                    reason => reason == null ? null : reason.Value,
                    value => value == null ? null : new PenaltyReason(value))
                .HasColumnName("Reason")
                .HasMaxLength(300);

            builder.Property(x => x.OccurredAt).IsRequired();

            builder.HasIndex(x => x.SessionId);
            builder.HasIndex(x => new { x.SessionId, x.TeamId });
        }
    }
}
