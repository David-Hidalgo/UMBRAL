using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MissionService.Domain.Entities;

namespace MissionService.Infrastructure.Persistence.Configurations
{
    public class TreasureHuntSubStageConfiguration : IEntityTypeConfiguration<TreasureHuntSubStage>
    {
        public void Configure(EntityTypeBuilder<TreasureHuntSubStage> builder)
        {
            builder.ToTable("TreasureHuntSubStages");

            builder.Property(x => x.Objective)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.SecretCode)
                .HasMaxLength(100)
                .IsRequired();

            // Ubicación del tesoro (opcional).
            builder.Property(x => x.Latitude);
            builder.Property(x => x.Longitude);
            builder.Property(x => x.SearchRadiusMeters);
        }
    }
}
