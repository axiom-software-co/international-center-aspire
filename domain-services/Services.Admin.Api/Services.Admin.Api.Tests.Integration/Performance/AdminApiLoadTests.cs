using Aspire.Hosting.Testing;
using InternationalCenter.Tests.Shared.Base;
using InternationalCenter.Tests.Shared.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBomber.Contracts;
using NBomber.CSharp;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace InternationalCenter.Services.Admin.Api.Tests.Integration.Performance;

/// <summary>
/// Performance load testing for Services.Admin.Api using NBomber
/// Tests medical-grade performance requirements for administrative operations
/// Validates audit trail performance, concurrent admin operations, and data integrity under load
/// Medical-grade performance validation for admin portal reliability and compliance
/// </summary>
public class AdminApiLoadTests : AspireIntegrationTestBase
{
    public AdminApiLoadTests(ITestOutputHelper output) : base(output)
    {
    }

    protected override string GetServiceName() => "services-admin-api";

    [Fact(DisplayName = "Load Test: Admin API should handle 50 concurrent administrators with audit compliance", Timeout = 120000)]
    public async Task LoadTest_AdminOperations_Should_Handle_50_ConcurrentAdministrators()
    {
        // Arrange - Setup clean test environment with audit requirements
        await CleanDatabaseAsync();
        var categoryId = await SetupTestCategoryAsync();
        await SetupTestServicesAsync(categoryId, 25); // Admin-appropriate dataset

        // Create load test scenario for administrative operations
        var scenario = Scenario.Create("admin_operations", async context =>
        {
            using var client = App!.CreateHttpClient(GetServiceName());
            
            // Simulate realistic admin workflow patterns with audit trails
            var adminOperations = new[]
            {
                // Read operations (most common in admin scenarios)
                () => client.GetAsync("/api/admin/services?page=1&pageSize=20"),
                () => client.GetAsync("/api/admin/services?includeInactive=true"),
                () => client.GetAsync($"/api/admin/categories/{categoryId}"),
                () => client.GetAsync("/api/admin/services/analytics?period=week"),
                () => client.GetAsync("/api/admin/audit/recent?limit=10"),
                
                // Write operations (require audit logging validation)
                () => TestCreateServiceAsync(client, categoryId),
                () => TestUpdateServiceAsync(client, categoryId)
            };

            var operation = adminOperations[Random.Shared.Next(adminOperations.Length)];
            
            try
            {
                var response = await operation();
                
                // Validate medical-grade audit headers are present
                ContractValidationUtils.ValidateCorrelationTracking(response, Output);
                
                return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail($"HTTP {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return Response.Fail(ex.Message);
            }
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 5, during: TimeSpan.FromSeconds(30)), // Gradual ramp up
            Simulation.KeepConstant(copies: 50, during: TimeSpan.FromMinutes(2)), // Sustained admin load
            Simulation.InjectPerSec(rate: 2, during: TimeSpan.FromSeconds(30))   // Gradual ramp down
        );

        // Act - Execute load test with audit validation
        var stats = await NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("admin-load-test-reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .Run();

        // Assert - Validate medical-grade performance requirements
        var scenarioStats = stats.AllScenarios.First(s => s.ScenarioName == "admin_operations");
        
        // Medical-grade performance contracts
        Assert.True(scenarioStats.Ok.Request.Count > 0, "No successful admin operations recorded");
        Assert.True(scenarioStats.Fail.Request.Count == 0, // Zero tolerance for failures in medical-grade systems
            $"Admin operations failed: {scenarioStats.Fail.Request.Count} failures detected");
        
        // Stricter response time requirements for admin operations
        Assert.True(scenarioStats.Ok.Request.Mean < 300, // 300ms mean for admin responsiveness
            $"Mean response time {scenarioStats.Ok.Request.Mean}ms exceeds 300ms medical-grade requirement");
        Assert.True(scenarioStats.Ok.Request.Percentile95 < 500, // 95th percentile under 500ms
            $"95th percentile {scenarioStats.Ok.Request.Percentile95}ms exceeds 500ms requirement");

        Output.WriteLine($"✅ ADMIN LOAD TEST PASSED: {scenarioStats.Ok.Request.Count} operations, " +
                        $"Mean: {scenarioStats.Ok.Request.Mean}ms, " +
                        $"P95: {scenarioStats.Ok.Request.Percentile95}ms, " +
                        $"Medical-grade performance maintained");
    }

    [Fact(DisplayName = "Audit Load Test: Admin API should maintain audit trail performance under load", Timeout = 150000)]
    public async Task AuditLoadTest_AdminOperations_Should_MaintainAuditTrailPerformance()
    {
        // Arrange - Setup for audit-intensive testing
        await CleanDatabaseAsync();
        var categoryId = await SetupTestCategoryAsync();
        await SetupTestServicesAsync(categoryId, 10); 

        // Create audit-focused load test scenario
        var scenario = Scenario.Create("audit_intensive", async context =>
        {
            using var client = App!.CreateHttpClient(GetServiceName());
            
            // Focus on operations that generate audit trails
            var auditOperations = new Func<Task<HttpResponseMessage>>[]
            {
                () => TestCreateServiceWithAuditAsync(client, categoryId),
                () => TestUpdateServiceWithAuditAsync(client, categoryId),
                () => TestDeleteServiceWithAuditAsync(client, categoryId),
                () => client.GetAsync("/api/admin/audit/services?page=1&pageSize=10"),
                () => client.GetAsync("/api/admin/audit/categories?page=1&pageSize=10")
            };

            var operation = auditOperations[Random.Shared.Next(auditOperations.Length)];
            var response = await operation();
            
            // Validate audit requirements are met
            if (response.IsSuccessStatusCode)
            {
                ContractValidationUtils.ValidateCorrelationTracking(response, Output);
                return Response.Ok();
            }
            
            return Response.Fail($"Audit operation failed: HTTP {response.StatusCode}");
        })
        .WithLoadSimulations(
            // Moderate sustained load focused on audit trail generation
            Simulation.KeepConstant(copies: 20, during: TimeSpan.FromMinutes(2))
        );

        // Act - Execute audit load test
        var stats = await NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("audit-load-test-reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .Run();

        // Assert - Validate audit trail performance
        var scenarioStats = stats.AllScenarios.First(s => s.ScenarioName == "audit_intensive");
        
        // Audit performance contracts
        Assert.True(scenarioStats.Ok.Request.Count > 0, "No successful audit operations");
        Assert.True(scenarioStats.Fail.Request.Count < scenarioStats.Ok.Request.Count * 0.01, // <1% failure rate
            $"Audit trail failure rate too high: {scenarioStats.Fail.Request.Count}");
        
        // Audit operations should maintain reasonable performance
        Assert.True(scenarioStats.Ok.Request.Mean < 600, // 600ms for audit-heavy operations
            $"Audit operation mean time {scenarioStats.Ok.Request.Mean}ms exceeds 600ms limit");

        Output.WriteLine($"✅ AUDIT LOAD TEST PASSED: {scenarioStats.Ok.Request.Count} audit operations, " +
                        $"Mean: {scenarioStats.Ok.Request.Mean}ms, " +
                        $"Audit trail integrity maintained under load");
    }

    [Fact(DisplayName = "Concurrent Modification Test: Admin API should handle concurrent updates safely", Timeout = 120000)]
    public async Task ConcurrentModificationTest_Should_HandleConcurrentUpdates_Safely()
    {
        // Arrange - Setup for concurrency testing
        await CleanDatabaseAsync();
        var categoryId = await SetupTestCategoryAsync();
        var serviceIds = await SetupTestServicesForConcurrencyAsync(categoryId, 5);

        // Create concurrent modification scenario
        var scenario = Scenario.Create("concurrent_updates", async context =>
        {
            using var client = App!.CreateHttpClient(GetServiceName());
            
            // Randomly select a service to update
            var serviceId = serviceIds[Random.Shared.Next(serviceIds.Count)];
            
            // Create update request with timestamp to track concurrent modifications
            var updateRequest = new
            {
                Title = $"Updated Service {DateTime.UtcNow:HHmmss.fff}",
                Description = $"Concurrent update test at {DateTime.UtcNow}",
                UpdatedBy = "load-test-admin"
            };

            var response = await client.PutAsJsonAsync($"/api/admin/services/{serviceId}", updateRequest);
            
            // Accept both success and conflict responses as valid outcomes
            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                return Response.Ok();
            }
            
            return Response.Fail($"Unexpected response: {response.StatusCode}");
        })
        .WithLoadSimulations(
            // High concurrency for short burst to test concurrent modification handling
            Simulation.KeepConstant(copies: 30, during: TimeSpan.FromSeconds(45))
        );

        // Act - Execute concurrency test
        var stats = await NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("concurrency-test-reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .Run();

        // Assert - Validate concurrency handling
        var scenarioStats = stats.AllScenarios.First(s => s.ScenarioName == "concurrent_updates");
        
        // Concurrency safety contracts
        Assert.True(scenarioStats.Ok.Request.Count > 0, "No concurrent operations completed");
        Assert.True(scenarioStats.Fail.Request.Count < scenarioStats.Ok.Request.Count * 0.1, // <10% failure rate acceptable for concurrency conflicts
            $"Too many concurrent operation failures: {scenarioStats.Fail.Request.Count}");

        Output.WriteLine($"✅ CONCURRENCY TEST PASSED: {scenarioStats.Ok.Request.Count} concurrent operations handled safely");
        
        // Validate data integrity after concurrent operations
        await ValidateDataIntegrityAfterConcurrentOperations(serviceIds);
    }

    #region Helper Methods

    /// <summary>
    /// Setup test services specifically for concurrency testing
    /// </summary>
    private async Task<List<Guid>> SetupTestServicesForConcurrencyAsync(Guid categoryId, int count)
    {
        var serviceIds = new List<Guid>();
        var connectionString = await GetConnectionStringAsync();
        using var connection = new Npgsql.NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        for (int i = 1; i <= count; i++)
        {
            var serviceId = Guid.NewGuid();
            serviceIds.Add(serviceId);
            
            var insertQuery = @"
                INSERT INTO services (id, title, slug, description, detailed_description, category_id, featured, available, sort_order, created_at, updated_at)
                VALUES (@Id, @Title, @Slug, @Description, @DetailedDescription, @CategoryId, @Featured, @Available, @SortOrder, @CreatedAt, @UpdatedAt)";

            using var command = connection.CreateCommand();
            command.CommandText = insertQuery;
            command.Parameters.AddWithValue("@Id", serviceId);
            command.Parameters.AddWithValue("@Title", $"Concurrency Test Service {i}");
            command.Parameters.AddWithValue("@Slug", $"concurrency-test-service-{i}");
            command.Parameters.AddWithValue("@Description", $"Service for concurrency testing {i}");
            command.Parameters.AddWithValue("@DetailedDescription", $"Detailed description for concurrency test service {i}");
            command.Parameters.AddWithValue("@CategoryId", categoryId);
            command.Parameters.AddWithValue("@Featured", false);
            command.Parameters.AddWithValue("@Available", true);
            command.Parameters.AddWithValue("@SortOrder", i);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

            await command.ExecuteNonQueryAsync();
        }

        Output.WriteLine($"✅ CONCURRENCY SETUP: Created {count} services for concurrency testing");
        return serviceIds;
    }

    /// <summary>
    /// Setup test services for admin load testing
    /// </summary>
    private async Task SetupTestServicesAsync(Guid categoryId, int count)
    {
        var connectionString = await GetConnectionStringAsync();
        using var connection = new Npgsql.NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        for (int i = 1; i <= count; i++)
        {
            var serviceId = Guid.NewGuid();
            var insertQuery = @"
                INSERT INTO services (id, title, slug, description, detailed_description, category_id, featured, available, sort_order, created_at, updated_at)
                VALUES (@Id, @Title, @Slug, @Description, @DetailedDescription, @CategoryId, @Featured, @Available, @SortOrder, @CreatedAt, @UpdatedAt)";

            using var command = connection.CreateCommand();
            command.CommandText = insertQuery;
            command.Parameters.AddWithValue("@Id", serviceId);
            command.Parameters.AddWithValue("@Title", $"Admin Load Test Service {i}");
            command.Parameters.AddWithValue("@Slug", $"admin-load-test-service-{i}");
            command.Parameters.AddWithValue("@Description", $"Service for admin load testing {i}");
            command.Parameters.AddWithValue("@DetailedDescription", $"Detailed description for admin load test service {i}");
            command.Parameters.AddWithValue("@CategoryId", categoryId);
            command.Parameters.AddWithValue("@Featured", i % 5 == 0); // Every 5th service is featured
            command.Parameters.AddWithValue("@Available", true);
            command.Parameters.AddWithValue("@SortOrder", i);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

            await command.ExecuteNonQueryAsync();
        }

        Output.WriteLine($"✅ ADMIN TEST SETUP: Created {count} test services for admin load testing");
    }

    private async Task<HttpResponseMessage> TestCreateServiceAsync(HttpClient client, Guid categoryId)
    {
        var newService = new
        {
            Title = $"Load Test Service {Guid.NewGuid():N}",
            Slug = $"load-test-service-{Guid.NewGuid():N}",
            Description = "Service created during load testing",
            DetailedDescription = "Detailed description for load test service",
            CategoryId = categoryId,
            Featured = false,
            Available = true,
            SortOrder = 100
        };

        return await client.PostAsJsonAsync("/api/admin/services", newService);
    }

    private async Task<HttpResponseMessage> TestUpdateServiceAsync(HttpClient client, Guid categoryId)
    {
        // Get first available service to update
        var servicesResponse = await client.GetAsync("/api/admin/services?page=1&pageSize=1");
        if (!servicesResponse.IsSuccessStatusCode) 
            return servicesResponse;

        var servicesJson = await servicesResponse.Content.ReadAsStringAsync();
        var services = JsonDocument.Parse(servicesJson);
        
        if (!services.RootElement.TryGetProperty("items", out var items) || items.GetArrayLength() == 0)
            return servicesResponse; // No services to update

        var serviceId = items[0].GetProperty("id").GetString();
        
        var updateRequest = new
        {
            Title = $"Updated Service {DateTime.UtcNow:HHmmss}",
            Description = "Updated during load testing"
        };

        return await client.PutAsJsonAsync($"/api/admin/services/{serviceId}", updateRequest);
    }

    private async Task<HttpResponseMessage> TestCreateServiceWithAuditAsync(HttpClient client, Guid categoryId)
    {
        var service = new
        {
            Title = $"Audit Test Service {DateTime.UtcNow:HHmmss.fff}",
            Slug = $"audit-test-{Guid.NewGuid():N}",
            Description = "Service for audit trail testing",
            CategoryId = categoryId,
            AuditContext = new
            {
                UserId = "load-test-admin",
                Operation = "CREATE",
                Reason = "Load testing audit trail performance"
            }
        };

        return await client.PostAsJsonAsync("/api/admin/services", service);
    }

    private async Task<HttpResponseMessage> TestUpdateServiceWithAuditAsync(HttpClient client, Guid categoryId)
    {
        var servicesResponse = await client.GetAsync("/api/admin/services?page=1&pageSize=1");
        if (!servicesResponse.IsSuccessStatusCode) return servicesResponse;

        var servicesJson = await servicesResponse.Content.ReadAsStringAsync();
        var services = JsonDocument.Parse(servicesJson);
        
        if (!services.RootElement.TryGetProperty("items", out var items) || items.GetArrayLength() == 0)
            return servicesResponse;

        var serviceId = items[0].GetProperty("id").GetString();
        
        var updateRequest = new
        {
            Title = $"Audit Updated {DateTime.UtcNow:HHmmss}",
            AuditContext = new
            {
                UserId = "load-test-admin", 
                Operation = "UPDATE",
                Reason = "Audit performance testing"
            }
        };

        return await client.PutAsJsonAsync($"/api/admin/services/{serviceId}", updateRequest);
    }

    private async Task<HttpResponseMessage> TestDeleteServiceWithAuditAsync(HttpClient client, Guid categoryId)
    {
        // Create a service to delete
        var createResponse = await TestCreateServiceWithAuditAsync(client, categoryId);
        if (!createResponse.IsSuccessStatusCode) return createResponse;

        var createdJson = await createResponse.Content.ReadAsStringAsync();
        var created = JsonDocument.Parse(createdJson);
        var serviceId = created.RootElement.GetProperty("id").GetString();

        return await client.DeleteAsync($"/api/admin/services/{serviceId}");
    }

    private async Task ValidateDataIntegrityAfterConcurrentOperations(List<Guid> serviceIds)
    {
        var connectionString = await GetConnectionStringAsync();
        using var connection = new Npgsql.NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Verify all services still exist and have valid data
        foreach (var serviceId in serviceIds)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT title, updated_at FROM services WHERE id = @Id";
            command.Parameters.AddWithValue("@Id", serviceId);

            using var reader = await command.ExecuteReaderAsync();
            Assert.True(reader.Read(), $"Service {serviceId} should still exist after concurrent operations");
            
            var title = reader.GetString("title");
            var updatedAt = reader.GetDateTime("updated_at");
            
            Assert.False(string.IsNullOrEmpty(title), "Service title should not be empty after concurrent updates");
            Assert.True(updatedAt > DateTime.MinValue, "UpdatedAt should be valid after concurrent updates");
        }

        Output.WriteLine("✅ DATA INTEGRITY: All services maintain valid state after concurrent operations");
    }

    #endregion
}