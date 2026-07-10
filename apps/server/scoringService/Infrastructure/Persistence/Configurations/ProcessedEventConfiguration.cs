using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoringService.Infrastructure.Persistence.Inbox;

namespace ScoringService.Infrastructure.Persistence.Configurations
{
    public sealed class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
    {
        public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
        {
            builder.ToTable("ProcessedEvents");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.RoutingKey).HasMaxLength(200).IsRequired();
            builder.Property(x => x.ProcessedAt).IsRequired();
        }
    }
}
