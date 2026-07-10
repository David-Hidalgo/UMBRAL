using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoringService.Domain.Entities;

namespace ScoringService.Infrastructure.Persistence.Configurations
{
    public sealed class SessionEventConfiguration : IEntityTypeConfiguration<SessionEvent>
    {
        public void Configure(EntityTypeBuilder<SessionEvent> builder)
        {
            builder.ToTable("SessionEvents");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.SessionId).IsRequired();
            builder.Property(x => x.Type).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
            builder.Property(x => x.Actor).HasMaxLength(200);
            builder.Property(x => x.OccurredAt).IsRequired();

            builder.HasIndex(x => x.SessionId);
        }
    }
}
