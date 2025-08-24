using InternationalCenter.Services.Public.Api.Application.UseCases;
using InternationalCenter.Services.Domain.Repositories;
using InternationalCenter.Services.Public.Api.Infrastructure.Repositories;
using InternationalCenter.Services.Domain.Infrastructure.Data;
using InternationalCenter.Services.Domain.Infrastructure.Interfaces;
using InternationalCenter.Shared.Infrastructure.Performance;
using InternationalCenter.Services.Migrations.Service;
using InternationalCenter.Shared.Infrastructure.Migrations;
using Microsoft.EntityFrameworkCore;

namespace InternationalCenter.Services.Public.Api.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        // Clean Architecture repositories following Microsoft patterns
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IServiceCategoryRepository, ServiceCategoryRepository>();
        
        // Services domain-specific migration services (other domains disabled for focused development)
        services.AddScoped<IServicesDomainMigrationService, ServicesDomainMigrationService>();
        services.AddScoped<IMigrationAuditService, MigrationAuditService>();
        
        // Services-only migration orchestration (simplified for vertical slice focus)
        services.AddScoped<IMigrationOrchestrationService, MigrationOrchestrationService>();
        services.AddScoped<IMigrationRollbackService, MigrationRollbackService>();
        services.AddScoped<IMigrationHealthMonitoringService, MigrationHealthMonitoringService>();
        
        return services;
    }
    
    public static IServiceCollection AddServicesDbContext(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ServicesDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly("InternationalCenter.Migrations.Service");
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

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // REST API Use Cases (replaced gRPC handlers)
        services.AddScoped<IServiceQueryUseCase, ServiceQueryUseCase>();
        services.AddScoped<GetServiceBySlugUseCase>(); // Keep separate for single service retrieval
        services.AddScoped<GetServiceCategoriesUseCase>(); // Keep separate for categories domain
        
        return services;
    }
}