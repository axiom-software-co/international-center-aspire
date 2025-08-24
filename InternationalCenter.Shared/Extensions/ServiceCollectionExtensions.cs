using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using InternationalCenter.Shared.Infrastructure;
using InternationalCenter.Shared.Services;
using Serilog;
using FluentValidation;
using System.Reflection;

namespace InternationalCenter.Shared.Extensions;

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

        // Logging
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        services.AddLogging(builder => builder.AddSerilog());

        // Validation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

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


    public static IServiceCollection AddServiceDefaults(this IServiceCollection services)
    {
        services.AddServiceDiscovery();
        services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
        });

        return services;
    }
}