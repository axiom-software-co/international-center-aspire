using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using InternationalCenter.Shared.Infrastructure;

namespace InternationalCenter.Migrations.Service;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // Use default connection string for design-time operations (will be overridden at runtime)
        // CRITICAL: Must match runtime configuration in Program.cs
        optionsBuilder.UseNpgsql("Host=localhost;Database=database;Username=postgres;Password=postgres", 
            npgsqlOptions =>
            {
                // Specify that migrations are in this assembly (Migration Service) - MUST match Program.cs
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContextFactory).Assembly.GetName().Name);
            });

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}