using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LiveSessionService.Domain.Entities;

namespace LiveSessionService.Infrastructure.Persistence.Configurations
{
    public class HintRequestConfiguration : IEntityTypeConfiguration<HintRequest>
    {
        public void Configure(EntityTypeBuilder<HintRequest> builder)
        {
            builder.ToTable("HintRequests");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.LiveSessionId)
                .IsRequired();

            builder.Property(x => x.TeamId)
                .IsRequired();

            builder.Property(x => x.SubStageId)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.RequestedAt)
                .IsRequired();

            builder.Property(x => x.ResolvedAt);

            // Acelera la búsqueda de la solicitud pendiente de un equipo por subetapa.
            builder.HasIndex(x => new { x.LiveSessionId, x.TeamId, x.SubStageId, x.Status });
        }
    }
}
