using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MissionService.Domain.Entities;

namespace MissionService.Infrastructure.Persistence.Configurations
{
    public class SubStageConfiguration : IEntityTypeConfiguration<SubStage>
    {
        public void Configure(EntityTypeBuilder<SubStage> builder)
        {
            builder.ToTable("SubStages");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.Order)
                .IsRequired();
        }
    }
}
