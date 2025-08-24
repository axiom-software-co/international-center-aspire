using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using InternationalCenter.Shared.Infrastructure;
using InternationalCenter.Services.Migrations.Service;

var builder = Host.CreateApplicationBuilder(args);

// Add service defaults
builder.AddServiceDefaults();

// Add database context
builder.AddNpgsqlDbContext<ApplicationDbContext>("database");

// Register migration services
builder.Services.AddScoped<IServicesDomainMigrationService, ServicesDomainMigrationService>();

var host = builder.Build();

// For demonstration purposes - in production this would be triggered differently
using (var scope = host.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var migrationService = scope.ServiceProvider.GetRequiredService<IServicesDomainMigrationService>();
    
    logger.LogInformation("Services Domain Migration Service started");
    
    try
    {
        await migrationService.ApplyMigrationsAsync();
        logger.LogInformation("Services Domain migrations completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Services Domain migration failed");
        throw;
    }
}

await host.RunAsync();