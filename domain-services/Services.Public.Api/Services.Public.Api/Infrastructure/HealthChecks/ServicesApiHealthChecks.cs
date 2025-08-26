using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using InternationalCenter.Services.Public.Api.Infrastructure.Repositories.Interfaces;
using InternationalCenter.Services.Domain.ValueObjects;
using InternationalCenter.Shared.Services;
using System.Diagnostics;

namespace InternationalCenter.Services.Public.Api.Infrastructure.HealthChecks;

/// <summary>
/// Health check for Services Public API - validates critical read functionality
/// Medical-grade monitoring ensuring Services Public API is fully operational
/// Tests Dapper read repositories and business logic for public endpoints
/// </summary>
public class ServicesApiHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ServicesApiHealthCheck> _logger;
    
    public ServicesApiHealthCheck(IServiceProvider serviceProvider, ILogger<ServicesApiHealthCheck> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var data = new Dictionary<string, object>();
        
        try
        {
            // Test Dapper read repositories (critical for public API performance)
            await TestReadRepositoryHealth(data, cancellationToken);
            
            // Test domain services availability
            await TestDomainServicesHealth(data, cancellationToken);
            
            stopwatch.Stop();
            data["check_duration_ms"] = stopwatch.ElapsedMilliseconds;
            data["timestamp"] = DateTime.UtcNow;
            
            _logger.LogInformation("HEALTH_CHECK: Services Public API health check completed in {Duration}ms - Status: Healthy", 
                stopwatch.ElapsedMilliseconds);
            
            return HealthCheckResult.Healthy("Services Public API is fully operational", data);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            data["error"] = ex.Message;
            data["check_duration_ms"] = stopwatch.ElapsedMilliseconds;
            data["timestamp"] = DateTime.UtcNow;
            
            _logger.LogError(ex, "HEALTH_CHECK: Services Public API health check failed after {Duration}ms", 
                stopwatch.ElapsedMilliseconds);
            
            return HealthCheckResult.Unhealthy("Services Public API is experiencing issues", ex, data);
        }
    }
    
    private async Task TestReadRepositoryHealth(Dictionary<string, object> data, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        
        try
        {
            // Test Dapper Service read repository (optimized for public API)
            var serviceReadRepo = scope.ServiceProvider.GetService<IServiceReadRepository>();
            if (serviceReadRepo != null)
            {
                // Test basic read operation (lightweight count query)
                var activeServices = await serviceReadRepo.GetActiveServicesAsync(cancellationToken);
                data["active_service_count"] = activeServices.Count();
                data["service_read_repository"] = "healthy";
                
                // Test featured services query
                var featuredServices = await serviceReadRepo.GetFeaturedServicesAsync(5, cancellationToken);
                data["featured_service_count"] = featuredServices.Count();
            }
            else
            {
                data["service_read_repository"] = "not_registered";
            }
            
            // Test Service Category read repository
            var categoryReadRepo = scope.ServiceProvider.GetService<IServiceCategoryReadRepository>();
            if (categoryReadRepo != null)
            {
                var activeCategories = await categoryReadRepo.GetActiveCategoriesAsync(cancellationToken);
                data["active_category_count"] = activeCategories.Count();
                data["category_read_repository"] = "healthy";
            }
            else
            {
                data["category_read_repository"] = "not_registered";
            }
        }
        catch (Exception ex)
        {
            data["read_repository_error"] = ex.Message;
            throw new InvalidOperationException("Read repository health check failed", ex);
        }
    }
    
    private async Task TestDomainServicesHealth(Dictionary<string, object> data, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        
        try
        {
            // Test version service (critical for production endpoints)
            var versionService = scope.ServiceProvider.GetService<IVersionService>();
            if (versionService != null)
            {
                var version = versionService.GetVersion();
                data["api_version"] = version;
                data["version_service"] = "healthy";
            }
            else
            {
                data["version_service"] = "not_registered";
            }
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            data["domain_services_error"] = ex.Message;
            throw new InvalidOperationException("Domain services health check failed", ex);
        }
    }
}