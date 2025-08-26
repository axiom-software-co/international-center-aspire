using Services.Shared.Infrastructure.Data;
using Services.Shared.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Services.Shared.Infrastructure.Extensions;

/// <summary>
/// Services Domain infrastructure service registrations
/// Centralized DbContext configuration following DDD Shared Kernel pattern
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers ServicesDbContext with standard configuration for all Services APIs
    /// Follows Microsoft recommended patterns for DbContext registration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="connectionString">PostgreSQL connection string</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddServicesDbContext(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ServicesDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly("Infrastructure.Database.Migrations.Service");
            });
            
            // Microsoft recommended optimizations
            options.EnableDetailedErrors();
            options.EnableServiceProviderCaching();
            options.EnableSensitiveDataLogging(false); // Never in production
        });
        
        // Register interface for dependency inversion
        services.AddScoped<IServicesDbContext>(provider => provider.GetRequiredService<ServicesDbContext>());
        
        return services;
    }
}