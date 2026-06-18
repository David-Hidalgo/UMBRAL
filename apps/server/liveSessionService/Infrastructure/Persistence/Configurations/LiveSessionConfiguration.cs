using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LiveSessionService.Domain.Entities;

namespace LiveSessionService.Infrastructure.Persistence.Configurations
{
    public class LiveSessionConfiguration : IEntityTypeConfiguration<LiveSession>
    {
        public void Configure(EntityTypeBuilder<LiveSession> builder)
        {
            builder.ToTable("LiveSessions");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.MissionId)
                .IsRequired();

            builder.Property(x => x.MissionName)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.ScheduledAt);

            builder.Property(x => x.CurrentStageIndex)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.HasMany(x => x.Teams)
                .WithOne()
                .HasForeignKey(x => x.LiveSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(x => x.Teams)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
