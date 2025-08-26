using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Services.Shared.Infrastructure.Data;

namespace Services.Shared.Migrations;

/// <summary>
/// Design-time factory for Services domain DbContext.
/// DOMAIN OWNERSHIP: Services domain manages its own migrations
/// </summary>
public class ServicesDbContextFactory : IDesignTimeDbContextFactory<ServicesDbContext>
{
    public ServicesDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ServicesDbContext>();
        
        // Use default connection string for design-time operations (will be overridden at runtime)
        // CRITICAL: Must match runtime configuration in Services APIs
        optionsBuilder.UseNpgsql("Host=localhost;Database=services_db;Username=postgres;Password=postgres", 
            npgsqlOptions =>
            {
                // Specify that migrations are in Services.Shared assembly 
                npgsqlOptions.MigrationsAssembly(typeof(ServicesDbContextFactory).Assembly.GetName().Name);
            });

        return new ServicesDbContext(optionsBuilder.Options);
    }
}