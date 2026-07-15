using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LiveSessionService.Domain.Entities;

namespace LiveSessionService.Infrastructure.Persistence.Configurations
{
    public class ReleasedHintConfiguration : IEntityTypeConfiguration<ReleasedHint>
    {
        public void Configure(EntityTypeBuilder<ReleasedHint> builder)
        {
            builder.ToTable("ReleasedHints");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.LiveSessionId)
                .IsRequired();

            builder.Property(x => x.TeamId)
                .IsRequired();

            builder.Property(x => x.SubStageId)
                .IsRequired();

            builder.Property(x => x.ClueId)
                .IsRequired();

            builder.Property(x => x.ClueText)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.ClueOrder)
                .IsRequired();

            builder.Property(x => x.ReleasedAt)
                .IsRequired();

            // RB-04: una pista no puede liberarse dos veces al mismo equipo en la misma sesión.
            builder.HasIndex(x => new { x.LiveSessionId, x.TeamId, x.ClueId })
                .IsUnique();
        }
    }
}
