using Microsoft.EntityFrameworkCore;
using ScreenTimeTracker.Models;

namespace ScreenTimeTracker.Data;

public class AppDbContext : DbContext
{
    public DbSet<TrackedApp> TrackedApps => Set<TrackedApp>();
    public DbSet<UsageSession> UsageSessions => Set<UsageSession>();
    public DbSet<TimeLimit> TimeLimits => Set<TimeLimit>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TrackedApp>(entity =>
        {
            entity.HasIndex(e => e.ProcessName).IsUnique();
            entity.HasOne(e => e.TimeLimit)
                  .WithOne(t => t.TrackedApp)
                  .HasForeignKey<TimeLimit>(t => t.TrackedAppId);
        });

        modelBuilder.Entity<UsageSession>(entity =>
        {
            entity.HasIndex(e => e.TrackedAppId);
            entity.HasIndex(e => e.StartTime);
            entity.HasIndex(e => new { e.TrackedAppId, e.StartTime });
        });

        modelBuilder.Entity<TimeLimit>(entity =>
        {
            entity.HasIndex(e => e.TrackedAppId).IsUnique();
        });
    }
}
