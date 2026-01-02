using Microsoft.EntityFrameworkCore;
using ProcessMonitorApi.Contracts;

namespace ProcessMonitorApi.Repository;

public class AppDbContext : DbContext
{
    // The constructor must accept DbContextOptions to allow DI configuration
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Define your tables here
    public DbSet<Analysis> Analyses => Set<Analysis>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Automatically finds all IEntityTypeConfiguration classes in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
