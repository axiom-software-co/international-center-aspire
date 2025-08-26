using InternationalCenter.Services.Public.Api.Application.UseCases;
using InternationalCenter.Services.Public.Api.Infrastructure.HealthChecks;
using InternationalCenter.Services.Domain.Repositories;
using InternationalCenter.Services.Public.Api.Infrastructure.Repositories;
using InternationalCenter.Services.Public.Api.Infrastructure.Repositories.Interfaces;
using InternationalCenter.Services.Public.Api.Infrastructure.Repositories.Dapper;
using InternationalCenter.Shared.Infrastructure.Performance;
using InternationalCenter.Services.Migrations.Service;
using InternationalCenter.Shared.Infrastructure.Migrations;

namespace InternationalCenter.Services.Public.Api.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        // Clean Architecture repositories following Microsoft patterns
        // EF Core repositories for write operations (used by Admin API)
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IServiceCategoryRepository, ServiceCategoryRepository>();
        
        // Dapper repositories for high-performance read operations (used by Public API)
        services.AddScoped<IServiceReadRepository, ServiceReadRepository>();
        services.AddScoped<IServiceCategoryReadRepository, ServiceCategoryReadRepository>();
        
        // Services domain-specific migration services (other domains disabled for focused development)
        services.AddScoped<IServicesDomainMigrationService, ServicesDomainMigrationService>();
        services.AddScoped<IMigrationAuditService, MigrationAuditService>();
        
        // Services-only migration orchestration (simplified for vertical slice focus)
        services.AddScoped<IMigrationOrchestrationService, MigrationOrchestrationService>();
        services.AddScoped<IMigrationRollbackService, MigrationRollbackService>();
        services.AddScoped<IMigrationHealthMonitoringService, MigrationHealthMonitoringService>();
        
        return services;
    }
    
    // AddServicesDbContext method moved to InternationalCenter.Services.Domain.Infrastructure.Extensions
    // Use: services.AddServicesDbContext(connectionString) from the domain infrastructure

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // REST API Use Cases (replaced gRPC handlers)
        services.AddScoped<IServiceQueryUseCase, ServiceQueryUseCase>();
        services.AddScoped<GetServiceBySlugUseCase>(); // Keep separate for single service retrieval
        services.AddScoped<GetServiceCategoriesUseCase>(); // Keep separate for categories domain
        
        return services;
    }
    
    public static IServiceCollection AddPublicHealthChecks(this IServiceCollection services)
    {
        // Add Services Public API specific health checks
        services.AddHealthChecks()
            .AddCheck<ServicesApiHealthCheck>("services-public-api", 
                tags: new[] { "api", "services", "critical" });
        
        return services;
    }
}