using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MissionService.Domain.Entities;

namespace MissionService.Infrastructure.Persistence.Configurations
{
    public class StageConfiguration : IEntityTypeConfiguration<Stage>
    {
        public void Configure(EntityTypeBuilder<Stage> builder)
        {
            builder.ToTable("Stages");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.Description)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.Order)
                .IsRequired();

            builder.HasMany(x => x.SubStages)
                .WithOne()
                .HasForeignKey(x => x.StageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(x => x.SubStages)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
