using Microsoft.EntityFrameworkCore;
using LiveSessionService.Domain.Entities;

namespace LiveSessionService.Infrastructure.Persistence
{
    public class LiveSessionDbContext : DbContext
    {
        public DbSet<LiveSession> LiveSessions { get; set; } = null!;
        public DbSet<Team> Teams { get; set; } = null!;
        public DbSet<Player> Players { get; set; } = null!;
        public DbSet<HintRequest> HintRequests { get; set; } = null!;
        public DbSet<ReleasedHint> ReleasedHints { get; set; } = null!;

        public LiveSessionDbContext(DbContextOptions<LiveSessionDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(LiveSessionDbContext).Assembly);
        }
    }
}
