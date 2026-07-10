using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoringService.Domain.Entities;

namespace ScoringService.Infrastructure.Persistence.Configurations
{
    public sealed class EvidenceSubmissionConfiguration : IEntityTypeConfiguration<EvidenceSubmission>
    {
        public void Configure(EntityTypeBuilder<EvidenceSubmission> builder)
        {
            builder.ToTable("EvidenceSubmissions");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.SessionId).IsRequired();
            builder.Property(x => x.TeamId).IsRequired();
            builder.Property(x => x.SubStageId).IsRequired();
            builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
            builder.Property(x => x.Payload).HasMaxLength(500).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(x => x.SubmittedAt).IsRequired();
            builder.Property(x => x.ValidatedAt);
            builder.Property(x => x.AwardedPoints);

            // Los eventos de dominio no se persisten.
            builder.Ignore(x => x.DomainEvents);

            builder.HasIndex(x => x.SessionId);
            builder.HasIndex(x => new { x.SessionId, x.TeamId });
        }
    }
}
