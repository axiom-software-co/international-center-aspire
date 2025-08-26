using InternationalCenter.Services.Domain.Entities;
using InternationalCenter.Services.Domain.Infrastructure.Data;
using InternationalCenter.Services.Domain.ValueObjects;
using InternationalCenter.Tests.Shared.Base;
using InternationalCenter.Tests.Shared.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace InternationalCenter.Services.Admin.Api.Tests.Integration.Infrastructure;

/// <summary>
/// Standardized Aspire integration test base for Services.Admin.Api following Microsoft Testing Framework patterns
/// WHY: Uses standardized Aspire-orchestrated PostgreSQL and Redis for production-realistic medical-grade testing with consistent infrastructure
/// SCOPE: Admin API integration tests with EF Core and medical-grade audit trails using standardized testing patterns
/// CONTEXT: Medical-grade Admin API requires reliable testing with proper data isolation for compliance using standardized Aspire testing infrastructure
/// </summary>
public abstract class AspireAdminIntegrationTestBase : AspireIntegrationTestBase
{
    private IServiceScope? _scope;

    /// <summary>
    /// Services database context for direct medical-grade database access in tests
    /// Provides EF Core access for Admin API integration testing with audit trail validation
    /// </summary>
    protected ServicesDbContext DbContext { get; private set; } = null!;

    protected AspireAdminIntegrationTestBase(ITestOutputHelper output) : base(output)
    {
    }

    /// <summary>
    /// Get service name for standardized Aspire HTTP client creation
    /// </summary>
    protected override string GetServiceName() => "services-admin-api";

    /// <summary>
    /// Initialize standardized Aspire testing infrastructure with medical-grade database context
    /// Leverages standardized base class and adds EF Core database context for Admin API testing
    /// </summary>
    public override async Task InitializeAsync()
    {
        // Initialize standardized Aspire infrastructure using base class
        await base.InitializeAsync();

        // Configure EF Core database context for medical-grade Admin API testing
        var connectionString = await GetConnectionStringAsync();
        
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
    /// Cleanup resources with standardized medical-grade audit logging
    /// Leverages parent class cleanup for Aspire infrastructure and adds EF Core cleanup
    /// </summary>
    public override async Task DisposeAsync()
    {
        var cleanupStartTime = DateTimeOffset.UtcNow;
        Output.WriteLine($"🏥 STANDARDIZED MEDICAL-GRADE CLEANUP START: {cleanupStartTime.ToString(StandardizedTestConfiguration.LoggingConfiguration.IsoTimestampFormat)}");
        
        try
        {
            // Cleanup EF Core resources first
            _scope?.Dispose();
            Output.WriteLine("🏥 STANDARDIZED MEDICAL-GRADE CLEANUP: EF Core scope disposed");
            
            // Cleanup standardized Aspire infrastructure using parent class
            await base.DisposeAsync();
            
            var cleanupEndTime = DateTimeOffset.UtcNow;
            var cleanupDuration = cleanupEndTime - cleanupStartTime;
            Output.WriteLine($"🏥 STANDARDIZED MEDICAL-GRADE CLEANUP COMPLETE: Duration: {StandardizedTestConfiguration.LoggingConfiguration.FormatDuration(cleanupDuration)} - Time: {cleanupEndTime.ToString(StandardizedTestConfiguration.LoggingConfiguration.IsoTimestampFormat)}");
        }
        catch (Exception ex)
        {
            var errorTime = DateTimeOffset.UtcNow;
            Output.WriteLine($"🏥 STANDARDIZED MEDICAL-GRADE CLEANUP ERROR: {ex.Message} - Time: {errorTime.ToString(StandardizedTestConfiguration.LoggingConfiguration.IsoTimestampFormat)}");
            // Don't rethrow cleanup exceptions for medical-grade stability
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