using Aspire.Hosting.Testing;
using InternationalCenter.Tests.Shared.Base;
using InternationalCenter.Tests.Shared.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBomber.Contracts;
using NBomber.CSharp;
using System.Net.Http.Json;
using Xunit;
using Xunit.Abstractions;

namespace InternationalCenter.Services.Public.Api.Tests.Integration.Performance;

/// <summary>
/// Performance load testing for Services.Public.Api using NBomber
/// Tests realistic load patterns for public website gateway scenarios
/// Validates performance under concurrent user load and traffic spikes
/// Medical-grade performance validation for public website reliability
/// </summary>
public class PublicApiLoadTests : AspireIntegrationTestBase
{
    public PublicApiLoadTests(ITestOutputHelper output) : base(output)
    {
    }

    protected override string GetServiceName() => "services-public-api";

    [Fact(DisplayName = "Load Test: Public API should handle 100 concurrent users browsing services", Timeout = 120000)]
    public async Task LoadTest_ServicesEndpoint_Should_Handle_100_ConcurrentUsers()
    {
        // Arrange - Setup clean test environment
        await CleanDatabaseAsync();
        var categoryId = await SetupTestCategoryAsync();
        await SetupTestServicesAsync(categoryId, 50); // Realistic dataset

        // Create load test scenario for service browsing
        var scenario = Scenario.Create("service_browsing", async context =>
        {
            using var client = App!.CreateHttpClient(GetServiceName());
            
            // Simulate realistic user browsing patterns
            var endpoints = new[]
            {
                "/api/services?page=1&pageSize=10",
                "/api/services?page=2&pageSize=10", 
                "/api/services/featured",
                $"/api/categories/{categoryId}/services",
                "/api/services?search=test&page=1&pageSize=5"
            };

            var randomEndpoint = endpoints[Random.Shared.Next(endpoints.Length)];
            
            var response = await client.GetAsync(randomEndpoint);
            
            return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 10, during: TimeSpan.FromSeconds(30)), // Ramp up
            Simulation.KeepConstant(copies: 100, during: TimeSpan.FromMinutes(2)), // Sustained load
            Simulation.InjectPerSec(rate: 5, during: TimeSpan.FromSeconds(30))   // Ramp down
        );

        // Act - Execute load test
        var stats = await NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("load-test-reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .Run();

        // Assert - Validate performance requirements
        var scenarioStats = stats.AllScenarios.First(s => s.ScenarioName == "service_browsing");
        
        // Performance contracts for public website
        Assert.True(scenarioStats.Ok.Request.Count > 0, "No successful requests recorded");
        Assert.True(scenarioStats.Fail.Request.Count < scenarioStats.Ok.Request.Count * 0.01, // <1% error rate
            $"Error rate too high: {scenarioStats.Fail.Request.Count}/{scenarioStats.Ok.Request.Count + scenarioStats.Fail.Request.Count}");
        
        // Response time contracts (public website performance requirements)
        Assert.True(scenarioStats.Ok.Request.Mean < 500, // 500ms mean response time
            $"Mean response time {scenarioStats.Ok.Request.Mean}ms exceeds 500ms requirement");
        Assert.True(scenarioStats.Ok.Request.Percentile95 < 1000, // 95th percentile under 1 second
            $"95th percentile {scenarioStats.Ok.Request.Percentile95}ms exceeds 1000ms requirement");

        Output.WriteLine($"✅ LOAD TEST PASSED: {scenarioStats.Ok.Request.Count} requests, " +
                        $"Mean: {scenarioStats.Ok.Request.Mean}ms, " +
                        $"P95: {scenarioStats.Ok.Request.Percentile95}ms");
    }

    [Fact(DisplayName = "Stress Test: Public API should handle traffic spikes gracefully", Timeout = 180000)]
    public async Task StressTest_ServicesEndpoint_Should_Handle_TrafficSpikes()
    {
        // Arrange - Setup test data
        await CleanDatabaseAsync();
        var categoryId = await SetupTestCategoryAsync();
        await SetupTestServicesAsync(categoryId, 100); // Larger dataset for stress testing

        // Create stress test scenario with traffic spikes
        var scenario = Scenario.Create("traffic_spike", async context =>
        {
            using var client = App!.CreateHttpClient(GetServiceName());
            
            var response = await client.GetAsync("/api/services?page=1&pageSize=20");
            
            // Validate response structure for load testing
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return !string.IsNullOrEmpty(content) ? Response.Ok() : Response.Fail("Empty response");
            }
            
            return Response.Fail($"HTTP {response.StatusCode}");
        })
        .WithLoadSimulations(
            // Simulate traffic spike patterns
            Simulation.InjectPerSec(rate: 5, during: TimeSpan.FromSeconds(20)),   // Normal traffic
            Simulation.InjectPerSec(rate: 50, during: TimeSpan.FromSeconds(30)),  // Traffic spike
            Simulation.InjectPerSec(rate: 100, during: TimeSpan.FromSeconds(20)), // Peak spike
            Simulation.InjectPerSec(rate: 10, during: TimeSpan.FromSeconds(30))   // Recovery
        );

        // Act - Execute stress test
        var stats = await NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("stress-test-reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .Run();

        // Assert - Validate stress test requirements
        var scenarioStats = stats.AllScenarios.First(s => s.ScenarioName == "traffic_spike");
        
        // Stress test acceptance criteria
        Assert.True(scenarioStats.Ok.Request.Count > 0, "No successful requests during stress test");
        Assert.True(scenarioStats.Fail.Request.Count < scenarioStats.Ok.Request.Count * 0.05, // <5% error rate under stress
            $"Error rate under stress too high: {scenarioStats.Fail.Request.Count}/{scenarioStats.Ok.Request.Count + scenarioStats.Fail.Request.Count}");
        
        // Response time degradation should be reasonable under stress
        Assert.True(scenarioStats.Ok.Request.Mean < 2000, // 2s mean under stress
            $"Mean response time under stress {scenarioStats.Ok.Request.Mean}ms exceeds 2000ms limit");

        Output.WriteLine($"✅ STRESS TEST PASSED: {scenarioStats.Ok.Request.Count} requests handled, " +
                        $"Mean: {scenarioStats.Ok.Request.Mean}ms, " +
                        $"Max: {scenarioStats.Ok.Request.Max}ms");
    }

    [Fact(DisplayName = "Endurance Test: Public API should maintain performance over extended periods", Timeout = 300000)]
    public async Task EnduranceTest_ServicesEndpoint_Should_MaintainPerformance_OverTime()
    {
        // Arrange - Setup for long-running test
        await CleanDatabaseAsync();
        var categoryId = await SetupTestCategoryAsync();
        await SetupTestServicesAsync(categoryId, 25); // Moderate dataset

        // Create endurance test scenario
        var scenario = Scenario.Create("endurance", async context =>
        {
            using var client = App!.CreateHttpClient(GetServiceName());
            
            // Mix of different operations for realistic usage
            var operations = new Func<Task<HttpResponseMessage>>[]
            {
                () => client.GetAsync("/api/services?page=1&pageSize=10"),
                () => client.GetAsync("/api/services/featured"),
                () => client.GetAsync($"/api/categories/{categoryId}/services"),
                () => client.GetAsync("/api/services?search=web&page=1&pageSize=5")
            };

            var operation = operations[Random.Shared.Next(operations.Length)];
            var response = await operation();
            
            return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
        })
        .WithLoadSimulations(
            // Sustained moderate load over 4 minutes
            Simulation.KeepConstant(copies: 25, during: TimeSpan.FromMinutes(4))
        );

        // Act - Execute endurance test
        var stats = await NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("endurance-test-reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .Run();

        // Assert - Validate endurance requirements
        var scenarioStats = stats.AllScenarios.First(s => s.ScenarioName == "endurance");
        
        // Endurance test acceptance criteria
        Assert.True(scenarioStats.Ok.Request.Count > 1000, // Should handle significant volume
            $"Insufficient requests processed: {scenarioStats.Ok.Request.Count}");
        Assert.True(scenarioStats.Fail.Request.Count < scenarioStats.Ok.Request.Count * 0.02, // <2% error rate
            $"Error rate during endurance too high: {scenarioStats.Fail.Request.Count}");
        
        // Performance should remain consistent over time
        Assert.True(scenarioStats.Ok.Request.Mean < 750, // 750ms mean over extended period
            $"Mean response time over endurance period {scenarioStats.Ok.Request.Mean}ms exceeds 750ms");

        Output.WriteLine($"✅ ENDURANCE TEST PASSED: {scenarioStats.Ok.Request.Count} requests over 4 minutes, " +
                        $"Mean: {scenarioStats.Ok.Request.Mean}ms, " +
                        $"Consistent performance maintained");
    }

    #region Helper Methods

    /// <summary>
    /// Setup test services for load testing
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
            command.Parameters.AddWithValue("@Title", $"Load Test Service {i}");
            command.Parameters.AddWithValue("@Slug", $"load-test-service-{i}");
            command.Parameters.AddWithValue("@Description", $"Description for load test service {i}");
            command.Parameters.AddWithValue("@DetailedDescription", $"Detailed description for comprehensive load testing service {i}");
            command.Parameters.AddWithValue("@CategoryId", categoryId);
            command.Parameters.AddWithValue("@Featured", i % 10 == 0); // Every 10th service is featured
            command.Parameters.AddWithValue("@Available", true);
            command.Parameters.AddWithValue("@SortOrder", i);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

            await command.ExecuteNonQueryAsync();
        }

        Output.WriteLine($"✅ TEST SETUP: Created {count} test services for load testing");
    }

    #endregion
}