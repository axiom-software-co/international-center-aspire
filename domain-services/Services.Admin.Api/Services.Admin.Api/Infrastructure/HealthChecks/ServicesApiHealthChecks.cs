using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using InternationalCenter.Services.Domain.Repositories;
using InternationalCenter.Services.Domain.ValueObjects;
using InternationalCenter.Services.Domain.Specifications;
using InternationalCenter.Shared.Services;
using System.Diagnostics;

namespace InternationalCenter.Services.Admin.Api.Infrastructure.HealthChecks;

/// <summary>
/// Health check for Services APIs - validates critical business functionality
/// Medical-grade monitoring ensuring Services APIs are fully operational
/// Tests Services domain repositories and business logic
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
            // Test repository availability (critical for both APIs)
            await TestRepositoryHealth(data, cancellationToken);
            
            // Test domain services availability
            await TestDomainServicesHealth(data, cancellationToken);
            
            stopwatch.Stop();
            data["check_duration_ms"] = stopwatch.ElapsedMilliseconds;
            data["timestamp"] = DateTime.UtcNow;
            
            _logger.LogInformation("HEALTH_CHECK: Services API health check completed in {Duration}ms - Status: Healthy", 
                stopwatch.ElapsedMilliseconds);
            
            return HealthCheckResult.Healthy("Services APIs are fully operational", data);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            data["error"] = ex.Message;
            data["check_duration_ms"] = stopwatch.ElapsedMilliseconds;
            data["timestamp"] = DateTime.UtcNow;
            
            _logger.LogError(ex, "HEALTH_CHECK: Services API health check failed after {Duration}ms", 
                stopwatch.ElapsedMilliseconds);
            
            return HealthCheckResult.Unhealthy("Services APIs are experiencing issues", ex, data);
        }
    }
    
    private async Task TestRepositoryHealth(Dictionary<string, object> data, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        
        try
        {
            // Test Service repository availability
            var serviceRepo = scope.ServiceProvider.GetService<IServiceRepository>();
            if (serviceRepo != null)
            {
                // Test basic repository operation (count query is lightweight)
                var serviceCount = await serviceRepo.CountAsync(new PublishedServicesSpecification(), cancellationToken);
                data["service_count"] = serviceCount;
                data["service_repository"] = "healthy";
            }
            else
            {
                data["service_repository"] = "not_registered";
            }
            
            // Test ServiceCategory repository availability
            var categoryRepo = scope.ServiceProvider.GetService<IServiceCategoryRepository>();
            if (categoryRepo != null)
            {
                var categories = await categoryRepo.GetActiveOrderedAsync(cancellationToken);
                data["category_count"] = categories.Count;
                data["category_repository"] = "healthy";
            }
            else
            {
                data["category_repository"] = "not_registered";
            }
        }
        catch (Exception ex)
        {
            data["repository_error"] = ex.Message;
            throw new InvalidOperationException("Repository health check failed", ex);
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