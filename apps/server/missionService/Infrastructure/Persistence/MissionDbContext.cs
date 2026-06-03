using Microsoft.EntityFrameworkCore;
using MissionService.Domain.Entities;

namespace MissionService.Infrastructure.Persistence
{
    public class MissionDbContext : DbContext
    {
        public DbSet<Mission> Missions { get; set; } = null!;
        public DbSet<Stage> Stages { get; set; } = null!;
        public DbSet<SubStage> SubStages { get; set; } = null!;
        public DbSet<TriviaSubStage> TriviaSubStages { get; set; } = null!;
        public DbSet<TreasureHuntSubStage> TreasureHuntSubStages { get; set; } = null!;
        public DbSet<Question> Questions { get; set; } = null!;
        public DbSet<Answer> Answers { get; set; } = null!;
        public DbSet<Clue> Clues { get; set; } = null!;

        public MissionDbContext(DbContextOptions<MissionDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(MissionDbContext).Assembly);
        }
    }
}
