using InternationalCenter.Services.Admin.Api.Application.UseCases;
using InternationalCenter.Services.Admin.Api.Handlers;
using InternationalCenter.Services.Admin.Api.Infrastructure.Repositories;
using InternationalCenter.Services.Domain.Repositories;
using InternationalCenter.Services.Domain.Infrastructure.Data;
using InternationalCenter.Services.Domain.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

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
}