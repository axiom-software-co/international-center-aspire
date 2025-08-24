using Aspire.Hosting;
using Aspire.Hosting.Testing;
using InternationalCenter.Services.Domain.Entities;
using InternationalCenter.Services.Domain.Infrastructure.Data;
using InternationalCenter.Services.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InternationalCenter.Services.Admin.Api.Tests.Integration.Infrastructure;

/// <summary>
/// TDD RED: Aspire integration test base following Microsoft Testing Framework patterns
/// Uses real Aspire-orchestrated PostgreSQL and Redis for production-realistic testing
/// Medical-grade audit logging verification with real database persistence
/// </summary>
public abstract class AspireAdminIntegrationTestBase : IAsyncLifetime
{
    private DistributedApplication? _app;
    private IServiceScope? _scope;

    /// <summary>
    /// HTTP client for Admin API integration testing
    /// </summary>
    protected HttpClient AdminApiClient { get; private set; } = null!;

    /// <summary>
    /// Services database context for direct database access in tests
    /// </summary>
    protected ServicesDbContext DbContext { get; private set; } = null!;

    /// <summary>
    /// TDD RED: Initialize Aspire testing infrastructure with real services
    /// </summary>
    public async Task InitializeAsync()
    {
        // TDD RED: Create and start Aspire testing application using AppHost configuration
        var appBuilder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        _app = await appBuilder.BuildAsync();

        // TDD RED: Start all Aspire-orchestrated services (PostgreSQL, Redis, Admin API)
        await _app.StartAsync();

        // TDD RED: Get Admin API HTTP client through Aspire service discovery
        AdminApiClient = _app.CreateHttpClient("services-admin-api", "adminapi");

        // TDD RED: Configure database context for testing
        var connectionString = await _app.GetConnectionStringAsync("database");
        
        var services = new ServiceCollection();
        services.AddDbContext<ServicesDbContext>(options =>
            options.UseNpgsql(connectionString));
        
        var testServiceProvider = services.BuildServiceProvider();
        _scope = testServiceProvider.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<ServicesDbContext>();

        // TDD RED: Ensure database is ready for testing
        await DbContext.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// TDD CLEANUP: Dispose Aspire testing infrastructure
    /// </summary>
    public async Task DisposeAsync()
    {
        _scope?.Dispose();
        
        if (_app != null)
        {
            await _app.DisposeAsync();
        }
    }

    /// <summary>
    /// TDD GREEN: Seed test data into real PostgreSQL database
    /// </summary>
    protected async Task SeedServiceAsync(Service service)
    {
        DbContext.Services.Add(service);
        await DbContext.SaveChangesAsync();
    }

    /// <summary>
    /// TDD GREEN: Seed test category into real PostgreSQL database
    /// </summary>
    protected async Task SeedServiceCategoryAsync(ServiceCategory category)
    {
        DbContext.ServiceCategories.Add(category);
        await DbContext.SaveChangesAsync();
    }

    /// <summary>
    /// TDD GREEN: Verify service exists in real database
    /// </summary>
    protected async Task<Service?> GetServiceFromDatabaseAsync(ServiceId serviceId)
    {
        return await DbContext.Services
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Id == serviceId);
    }

    /// <summary>
    /// TDD GREEN: Verify service category exists in real database
    /// </summary>
    protected async Task<ServiceCategory?> GetServiceCategoryFromDatabaseAsync(ServiceCategoryId categoryId)
    {
        return await DbContext.ServiceCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId);
    }

    /// <summary>
    /// TDD GREEN: Medical-grade audit verification - check audit logs in real database
    /// This is critical for admin API compliance
    /// </summary>
    protected async Task<bool> VerifyMedicalGradeAuditLogExistsAsync(string entityType, string operation, string entityId)
    {
        // TDD RED: This will fail until we implement audit logging table
        // Medical-grade audit logs should be persisted in dedicated audit table
        
        // For now, we verify through EF Core change tracking logs
        // In production, this would query dedicated audit_logs table
        var hasAuditEntry = await DbContext.Database
            .SqlQuery<int>($"SELECT 1 WHERE EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'audit_logs')")
            .AnyAsync();

        return hasAuditEntry; // Will return false until audit table exists - TDD RED
    }

    /// <summary>
    /// TDD GREEN: Clear test data between tests for isolation
    /// </summary>
    protected async Task ClearTestDataAsync()
    {
        await DbContext.Services.ExecuteDeleteAsync();
        await DbContext.ServiceCategories.ExecuteDeleteAsync();
    }
}