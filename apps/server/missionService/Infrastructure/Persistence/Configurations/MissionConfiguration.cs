using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MissionService.Domain.Entities;
using MissionService.Domain.ValueObjects;

namespace MissionService.Infrastructure.Persistence.Configurations
{
    public class MissionConfiguration : IEntityTypeConfiguration<Mission>
    {
        public void Configure(EntityTypeBuilder<Mission> builder)
        {
            builder.ToTable("Missions");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.Description)
                .HasMaxLength(1000)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion(
                    s => s.Value,
                    v => MissionStatus.FromString(v))
                .HasMaxLength(50)
                .IsRequired();

            builder.HasMany(x => x.Stages)
                .WithOne()
                .HasForeignKey(x => x.MissionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(x => x.Stages)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
