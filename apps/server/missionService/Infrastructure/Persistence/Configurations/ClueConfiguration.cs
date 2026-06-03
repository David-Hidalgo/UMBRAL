using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MissionService.Domain.Entities;

namespace MissionService.Infrastructure.Persistence.Configurations
{
    public class ClueConfiguration : IEntityTypeConfiguration<Clue>
    {
        public void Configure(EntityTypeBuilder<Clue> builder)
        {
            builder.ToTable("Clues");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Text)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.Order)
                .IsRequired();

            builder.Property(x => x.PenaltyPoints)
                .IsRequired();
        }
    }
}
