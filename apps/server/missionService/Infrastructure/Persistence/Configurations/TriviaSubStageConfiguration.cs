using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MissionService.Domain.Entities;

namespace MissionService.Infrastructure.Persistence.Configurations
{
    public class TriviaSubStageConfiguration : IEntityTypeConfiguration<TriviaSubStage>
    {
        public void Configure(EntityTypeBuilder<TriviaSubStage> builder)
        {
            builder.ToTable("TriviaSubStages");

            builder.HasOne(x => x.Question)
                .WithOne()
                .HasForeignKey<Question>(x => x.TriviaSubStageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(x => x.Question)
                .UsePropertyAccessMode(PropertyAccessMode.Property);
        }
    }
}
