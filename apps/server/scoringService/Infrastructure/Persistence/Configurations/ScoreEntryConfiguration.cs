using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoringService.Domain.Entities;
using ScoringService.Domain.ValueObjects;

namespace ScoringService.Infrastructure.Persistence.Configurations
{
    public sealed class ScoreEntryConfiguration : IEntityTypeConfiguration<ScoreEntry>
    {
        public void Configure(EntityTypeBuilder<ScoreEntry> builder)
        {
            builder.ToTable("ScoreEntries");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.SessionId).IsRequired();
            builder.Property(x => x.TeamId).IsRequired();

            // Value Object ScoreValue mapeado con ComplexProperty: se aplana a la columna "Points"
            // (backend-spec §2).
            builder.ComplexProperty(x => x.Value, v =>
            {
                v.Property(p => p.Points).HasColumnName("Points").IsRequired();
            });

            builder.Property(x => x.Origin).HasConversion<string>().HasMaxLength(30).IsRequired();
            builder.Property(x => x.SourceSubmissionId);

            // Value Object PenaltyReason (nullable) mapeado con value converter.
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
