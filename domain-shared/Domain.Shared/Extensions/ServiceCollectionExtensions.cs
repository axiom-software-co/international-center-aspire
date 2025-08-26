using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Shared.Infrastructure;
using Shared.Services;

namespace Shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Database registration removed - following Microsoft's pattern:
        // Each API service should use builder.AddNpgsqlDbContext<ApplicationDbContext>("connection-name")

        // Caching with Redis
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("redis")
                ?? throw new InvalidOperationException("Redis connection string not found");
            options.InstanceName = configuration["Cache:InstanceName"] ?? "InternationalCenter";
        });

        services.AddScoped<ICachingService, RedisCachingService>();

        // Logging - Configure Serilog via ILogger<T> dependency injection
        services.AddSerilog(config =>
        {
            config.ReadFrom.Configuration(configuration);
        });

        // Version service for production endpoint versioning
        services.AddSingleton<IVersionService, VersionService>();
        
        // Medical-grade audit system
        services.AddMedicalGradeAuditWithDefaults();

        // CORS
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
                    ?? new[] { "http://localhost:4321", "http://localhost:3000" };
                
                builder
                    .WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        // Health checks - database health check handled by individual APIs using AddNpgsqlDbContext
        var healthChecks = services.AddHealthChecks();
            
        var garnetConnectionString = configuration.GetConnectionString("garnet");
        if (!string.IsNullOrEmpty(garnetConnectionString))
        {
            healthChecks.AddRedis(garnetConnectionString);
        }

        return services;
    }


}