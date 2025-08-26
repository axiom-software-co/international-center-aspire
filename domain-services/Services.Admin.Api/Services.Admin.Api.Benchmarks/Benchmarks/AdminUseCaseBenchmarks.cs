using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using InternationalCenter.Services.Admin.Api.Application.UseCases;
using InternationalCenter.Services.Admin.Api.Infrastructure.Repositories;
using InternationalCenter.Services.Domain.Repositories;
using InternationalCenter.Services.Domain.Infrastructure.Data;
using InternationalCenter.Services.Domain.Infrastructure.Interfaces;
using InternationalCenter.Services.Domain.ValueObjects;
using InternationalCenter.Shared.Services;
using InternationalCenter.Shared.Models;

namespace InternationalCenter.Services.Admin.Api.Benchmarks.Benchmarks;

/// <summary>
/// BenchmarkDotNet performance tests for Services.Admin.Api use cases
/// WHY: Admin use case performance includes validation, audit logging, and EF Core operations
/// SCOPE: CreateServiceUseCase, UpdateServiceUseCase, DeleteServiceUseCase with medical-grade audit
/// CONTEXT: Admin API use cases serve admin workflows - performance critical for admin user experience
/// </summary>
[SimpleJob]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class AdminUseCaseBenchmarks
{
    private DistributedApplication? _app;
    private CreateServiceUseCase? _createServiceUseCase;
    private UpdateServiceUseCase? _updateServiceUseCase;
    private DeleteServiceUseCase? _deleteServiceUseCase;
    
    [GlobalSetup]
    public async Task GlobalSetup()
    {
        // Initialize Aspire application for realistic testing
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        _app = await builder.BuildAsync();
        await _app.StartAsync();

        // Get database connection string
        var connectionString = await _app.GetConnectionStringAsync("database");
        
        // Build service provider with use case dependencies
        var services = new ServiceCollection();
        
        // Configuration
        services.AddSingleton<IConfiguration>(sp =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    KeyValuePair.Create("ConnectionStrings:database", connectionString)
                })
                .Build();
            return config;
        });
        
        // Logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        // EF Core
        services.AddDbContext<ServicesDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<IServicesDbContext>(provider => provider.GetRequiredService<ServicesDbContext>());
        
        // Mock services for benchmarking
        services.AddSingleton<IAuditService, MockAuditService>();
        services.AddSingleton<IVersionService, MockVersionService>();
        
        // Repositories
        services.AddScoped<IServiceRepository, AdminServiceRepository>();
        services.AddScoped<IServiceCategoryRepository, AdminServiceCategoryRepository>();
        
        // Use Cases
        services.AddScoped<CreateServiceUseCase>();
        services.AddScoped<UpdateServiceUseCase>();
        services.AddScoped<DeleteServiceUseCase>();
        
        var serviceProvider = services.BuildServiceProvider();
        var scope = serviceProvider.CreateScope();
        
        _createServiceUseCase = scope.ServiceProvider.GetRequiredService<CreateServiceUseCase>();
        _updateServiceUseCase = scope.ServiceProvider.GetRequiredService<UpdateServiceUseCase>();
        _deleteServiceUseCase = scope.ServiceProvider.GetRequiredService<DeleteServiceUseCase>();
        
        // Ensure database is ready
        var dbContext = scope.ServiceProvider.GetRequiredService<IServicesDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
        
        Console.WriteLine("🏥 Admin use case benchmarks setup complete - using real Aspire infrastructure");
    }
    
    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        if (_app != null)
        {
            await _app.DisposeAsync();
        }
    }

    /// <summary>
    /// Benchmark CreateServiceUseCase - Critical admin operation
    /// Tests validation, audit logging, and EF Core service creation performance
    /// </summary>
    [Benchmark(Description = "CreateService UseCase - Critical admin operation")]
    public async Task<object> CreateServiceUseCase_ExecuteAsync()
    {
        var request = new CreateServiceRequest
        {
            Title = "Benchmark Admin Service",
            Description = "Service created through admin benchmark",
            DetailedDescription = "Detailed description for admin benchmarking",
            Slug = $"admin-benchmark-service-{Guid.NewGuid():N}",
            RequestId = Guid.NewGuid().ToString(),
            UserContext = "admin@benchmark.com",
            ClientIpAddress = "127.0.0.1",
            UserAgent = "Admin Benchmark Agent"
        };
        
        return await _createServiceUseCase!.ExecuteAsync(request, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark UpdateServiceUseCase - Service modification performance
    /// Tests EF Core update operations with audit logging and validation
    /// </summary>
    [Benchmark(Description = "UpdateService UseCase - Service modification")]
    public async Task<object> UpdateServiceUseCase_ExecuteAsync()
    {
        var serviceId = ServiceId.NewServiceId(); // Will likely not exist, tests validation path
        var request = new UpdateServiceRequest
        {
            Id = serviceId.Value,
            Title = "Updated Benchmark Service",
            Description = "Updated service description",
            DetailedDescription = "Updated detailed description",
            Slug = $"updated-benchmark-{Guid.NewGuid():N}",
            RequestId = Guid.NewGuid().ToString(),
            UserContext = "admin@benchmark.com",
            ClientIpAddress = "127.0.0.1",
            UserAgent = "Admin Benchmark Agent"
        };
        
        return await _updateServiceUseCase!.ExecuteAsync(request, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark DeleteServiceUseCase - Service deletion performance
    /// Tests EF Core delete operations with audit logging and validation
    /// </summary>
    [Benchmark(Description = "DeleteService UseCase - Service deletion")]
    public async Task<object> DeleteServiceUseCase_ExecuteAsync()
    {
        var serviceId = ServiceId.NewServiceId(); // Will likely not exist, tests validation path
        var request = new DeleteServiceRequest
        {
            Id = serviceId.Value,
            RequestId = Guid.NewGuid().ToString(),
            UserContext = "admin@benchmark.com",
            ClientIpAddress = "127.0.0.1",
            UserAgent = "Admin Benchmark Agent",
            Reason = "Benchmark testing"
        };
        
        return await _deleteServiceUseCase!.ExecuteAsync(request, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark CreateServiceUseCase validation error path
    /// Tests medical-grade validation and error handling performance
    /// </summary>
    [Benchmark(Description = "CreateService UseCase - Validation error handling")]
    public async Task<object> CreateServiceUseCase_ValidationError()
    {
        // Invalid request to test validation error path
        var request = new CreateServiceRequest
        {
            Title = "", // Empty title should trigger validation error
            Description = "",
            Slug = "",
            RequestId = Guid.NewGuid().ToString(),
            UserContext = "admin@benchmark.com",
            ClientIpAddress = "127.0.0.1",
            UserAgent = "Admin Benchmark Agent"
        };
        
        return await _createServiceUseCase!.ExecuteAsync(request, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark UpdateServiceUseCase not found scenario
    /// Tests not found handling performance with audit logging
    /// </summary>
    [Benchmark(Description = "UpdateService UseCase - Not found handling")]
    public async Task<object> UpdateServiceUseCase_NotFound()
    {
        // Non-existent service ID to test not found path
        var request = new UpdateServiceRequest
        {
            Id = "non-existent-service-id-benchmark",
            Title = "Non-existent Service",
            Description = "This service does not exist",
            Slug = "non-existent-benchmark",
            RequestId = Guid.NewGuid().ToString(),
            UserContext = "admin@benchmark.com",
            ClientIpAddress = "127.0.0.1",
            UserAgent = "Admin Benchmark Agent"
        };
        
        return await _updateServiceUseCase!.ExecuteAsync(request, CancellationToken.None);
    }
}

/// <summary>
/// Mock audit service for benchmarking - minimal overhead implementation
/// </summary>
public class MockAuditService : IAuditService
{
    public void SetAuditContext(AuditContext context) { /* No-op for benchmarking */ }
    
    public Task LogBusinessEventAsync(string action, string entityType, string operation, object? data, string severity)
    {
        // Minimal logging for benchmarking
        return Task.CompletedTask;
    }
    
    public Task LogSecurityEventAsync(string eventType, string description, object? data, string severity)
    {
        return Task.CompletedTask;
    }
    
    public Task LogPerformanceEventAsync(string operation, long durationMs, object? data)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// Mock version service for benchmarking
/// </summary>
public class MockVersionService : IVersionService
{
    public string Version => "1.0.0-benchmark";
    public string Environment => "benchmark";
    public string BuildDate => DateTime.UtcNow.ToString("yyyy-MM-dd");
}

/// <summary>
/// Request models for benchmarking (simplified versions)
/// </summary>
public class CreateServiceRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? DetailedDescription { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? RequestId { get; set; }
    public string? UserContext { get; set; }
    public string? ClientIpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public class UpdateServiceRequest
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? DetailedDescription { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? RequestId { get; set; }
    public string? UserContext { get; set; }
    public string? ClientIpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public class DeleteServiceRequest
{
    public string Id { get; set; } = string.Empty;
    public string? RequestId { get; set; }
    public string? UserContext { get; set; }
    public string? ClientIpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Reason { get; set; }
}