using InternationalCenter.Services.Admin.Api.Application.UseCases;
using InternationalCenter.Services.Admin.Api.Handlers;
using InternationalCenter.Services.Admin.Api.Infrastructure.Repositories;
using InternationalCenter.Services.Admin.Api.Infrastructure.HealthChecks;
using InternationalCenter.Services.Domain.Repositories;
using Services.Admin.Api.Infrastructure.Services;
using InternationalCenter.Services.Domain.Infrastructure.Data;
using Infrastructure.Metrics.Abstractions;

namespace InternationalCenter.Services.Admin.Api.Infrastructure.Extensions;

/// <summary>
/// Admin API service collection extensions with medical-grade dependencies
/// Uses shared domain library following DDD Shared Kernel pattern
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAdminDomainServices(this IServiceCollection services)
    {
        // Add Admin-specific repositories with medical-grade audit logging
        services.AddScoped<IServiceRepository, AdminServiceRepository>();
        services.AddScoped<IServiceCategoryRepository, AdminServiceCategoryRepository>();
        
        return services;
    }

    public static IServiceCollection AddAdminApplicationServices(this IServiceCollection services)
    {
        // Add Admin Use Cases following TDD approach
        services.AddScoped<ICreateServiceUseCase, CreateServiceUseCase>();
        services.AddScoped<IUpdateServiceUseCase, UpdateServiceUseCase>();
        services.AddScoped<IDeleteServiceUseCase, DeleteServiceUseCase>();
        
        // Add Admin Handlers for orchestrating Use Cases in presentation layer
        services.AddScoped<ServiceHandlers>();
        
        return services;
    }
    
    public static IServiceCollection AddAdminHealthChecks(this IServiceCollection services)
    {
        // Add Services Admin API specific health checks
        services.AddHealthChecks()
            .AddCheck<ServicesApiHealthCheck>("services-admin-api", 
                tags: new[] { "api", "services", "critical" });
        
        return services;
    }
    
    public static IServiceCollection AddAdminMetricsServices(this IServiceCollection services)
    {
        // Add Services Admin API metrics service for medical-grade compliance monitoring
        services.AddSingleton<ServicesAdminApiMetricsService>();
        
        // Add EF Core metrics wrapper for automatic performance tracking
        services.AddScoped<EfCoreMetricsWrapper<ServicesDbContext>>();
        
        return services;
    }
    
    // AddServicesDbContext method moved to InternationalCenter.Services.Domain.Infrastructure.Extensions
    // Use: services.AddServicesDbContext(connectionString) from the domain infrastructure
}