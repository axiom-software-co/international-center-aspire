using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Infrastructure;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // Use default connection string for design-time operations (will be overridden at runtime)
        optionsBuilder.UseNpgsql("Host=localhost;Database=database;Username=postgres;Password=postgres");

        // Create minimal service provider for design-time operations
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        return new ApplicationDbContext(optionsBuilder.Options, serviceProvider);
    }
}