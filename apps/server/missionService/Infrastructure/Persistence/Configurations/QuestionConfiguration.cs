using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MissionService.Domain.Entities;

namespace MissionService.Infrastructure.Persistence.Configurations
{
    public class QuestionConfiguration : IEntityTypeConfiguration<Question>
    {
        public void Configure(EntityTypeBuilder<Question> builder)
        {
            builder.ToTable("Questions");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Text)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.Points)
                .IsRequired();

            builder.HasMany(x => x.Answers)
                .WithOne()
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(x => x.Answers)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
