using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LiveSessionService.Domain.Entities;

namespace LiveSessionService.Infrastructure.Persistence.Configurations
{
    public class PlayerConfiguration : IEntityTypeConfiguration<Player>
    {
        public void Configure(EntityTypeBuilder<Player> builder)
        {
            builder.ToTable("Players");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.KeycloakUserId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.DisplayName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.TeamId)
                .IsRequired();

            builder.Property(x => x.JoinedAt)
                .IsRequired();

            // Un usuario solo puede registrarse una vez por equipo
            builder.HasIndex(x => new { x.TeamId, x.KeycloakUserId }).IsUnique();
        }
    }
}
