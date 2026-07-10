using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoringService.Domain.Entities;
using ScoringService.Domain.ValueObjects;

namespace ScoringService.Infrastructure.Persistence.Configurations
{
    public sealed class SessionMonitorConfiguration : IEntityTypeConfiguration<SessionMonitor>
    {
        public void Configure(EntityTypeBuilder<SessionMonitor> builder)
        {
            builder.ToTable("SessionMonitors");

            // La clave primaria es el propio SessionId (espejo 1:1 de la sesión en vivo).
            builder.HasKey(x => x.SessionId);
            builder.Property(x => x.SessionId).ValueGeneratedNever();

            builder.Property(x => x.MissionId).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

            // Colección primitiva de substages activos, mapeada desde el campo de respaldo a un
            // arreglo nativo uuid[] de PostgreSQL. La propiedad pública de solo lectura se ignora.
            builder.Ignore(x => x.ActiveSubStageIds);
            builder.Property<List<Guid>>("_activeSubStageIds")
                .HasColumnName("ActiveSubStageIds");

            // Equipos observados: entidad propiedad (owned) — skill efcore-postgres §1.
            // Se deja que EF gestione la clave (sombra autogenerada) en lugar de una clave natural
            // compuesta: así, al cargar un SessionMonitor existente y agregarle un equipo en otra
            // transacción (flujo del consumidor de eventos), EF lo detecta como INSERT y no como
            // UPDATE. La unicidad de equipos por sesión ya la garantiza SessionMonitor.RegisterTeam.
            builder.OwnsMany(x => x.Teams, tb =>
            {
                tb.ToTable("MonitoredTeams");

                tb.Property(t => t.TeamId).IsRequired();

                // Value Object TeamCode mapeado con value converter.
                tb.Property(t => t.Code)
                    .HasConversion(code => code.Value, value => new TeamCode(value))
                    .HasColumnName("Code")
                    .HasMaxLength(100)
                    .IsRequired();
            });

            builder.Navigation(x => x.Teams)
                .HasField("_teams")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
