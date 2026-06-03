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

            builder.HasMany(x => x.Clues)
                .WithOne()
                .HasForeignKey(x => x.TreasureHuntSubStageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(x => x.Clues)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
