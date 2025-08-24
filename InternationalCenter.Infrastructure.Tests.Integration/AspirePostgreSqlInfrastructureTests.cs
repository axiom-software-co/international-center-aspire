using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Microsoft.Extensions.Configuration;
using Polly;
using Dapper;
using Aspire.Hosting.Testing;
using InternationalCenter.Shared.Infrastructure;
using Xunit;
using InternationalCenter.Tests.Shared.TestCollections;

namespace InternationalCenter.Infrastructure.Tests.Integration;

/// <summary>
/// TDD GREEN Phase: Comprehensive PostgreSQL infrastructure tests using Aspire orchestration
/// Tests authentication, connection pooling, resilience, and medical-grade standards
/// against the actual Aspire-managed PostgreSQL instance
/// </summary>
[Collection("AspireInfrastructureTests")]
public class AspirePostgreSqlInfrastructureTests : IAsyncLifetime
{
    private readonly ILogger<AspirePostgreSqlInfrastructureTests> _logger;

    public AspirePostgreSqlInfrastructureTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<AspirePostgreSqlInfrastructureTests>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "TDD GREEN: Aspire PostgreSQL Authentication - Should handle auth failures gracefully")]
    public async Task AspirePostgreSQL_Authentication_Should_Handle_Invalid_Credentials_Gracefully()
    {
        // ARRANGE: Create Aspire application for testing using Microsoft documented pattern
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        var connectionString = await app.GetConnectionStringAsync("database");
        Assert.NotNull(connectionString);
        
        // Create invalid connection string
        var invalidConnectionString = connectionString.Replace("postgres", "invalid_user");
        
        var authenticationResilienceService = new PostgreSqlAuthenticationResilienceService();
        
        // ACT & ASSERT: Should gracefully handle authentication failures with retry logic
        var result = await authenticationResilienceService.TryConnectWithRetryAsync(
            invalidConnectionString, 
            maxRetries: 3,
            backoffMultiplier: 2.0);
            
        Assert.False(result);
        
        _logger.LogInformation("TDD GREEN: Authentication resilience working correctly with Aspire orchestration");
    }

    [Fact(DisplayName = "TDD GREEN: Aspire PostgreSQL Connection Pooling - Should manage connections efficiently")]
    public async Task AspirePostgreSQL_Connection_Pooling_Should_Handle_Concurrent_Connections()
    {
        // ARRANGE: Create Aspire application for testing using Microsoft documented pattern
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        var connectionString = await app.GetConnectionStringAsync("database");
        Assert.NotNull(connectionString);
        
        const int concurrentConnections = 50;
        var connectionPoolingService = new PostgreSqlConnectionPoolingService();
        
        // ACT & ASSERT: Should implement intelligent connection pooling
        var tasks = Enumerable.Range(0, concurrentConnections)
            .Select(async i => await connectionPoolingService.ExecuteWithManagedConnectionAsync(
                connectionString,
                async connection => await connection.QuerySingleAsync<int>("SELECT 1")))
            .ToArray();
        
        var results = await Task.WhenAll(tasks);
        Assert.All(results, result => Assert.Equal(1, result));
        
        _logger.LogInformation("TDD GREEN: Connection pooling working correctly with {ConnectionCount} concurrent connections through Aspire", concurrentConnections);
    }

    [Fact(DisplayName = "TDD GREEN: Aspire PostgreSQL Service Resilience - Should handle service restarts gracefully")]
    public async Task AspirePostgreSQL_Service_Resilience_Should_Handle_Aspire_Service_Restarts()
    {
        // ARRANGE: Create Aspire application for testing using Microsoft documented pattern
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        var connectionString = await app.GetConnectionStringAsync("database");
        Assert.NotNull(connectionString);
        
        var lifecycleService = new PostgreSqlLifecycleResilienceService();
        
        // ACT: Test connection establishment through Aspire
        await lifecycleService.EstablishHealthyConnectionAsync(connectionString);
        
        // Simulate service restart by testing reconnection capability
        var reconnectionResult = await lifecycleService.ValidateConnectionAfterRestartAsync(connectionString);
        Assert.True(reconnectionResult);
        
        _logger.LogInformation("TDD GREEN: Service resilience working correctly through Aspire orchestration");
    }

    [Fact(DisplayName = "TDD GREEN: Aspire PostgreSQL Transaction Handling - Should rollback on failure")]
    public async Task AspirePostgreSQL_Transaction_Handling_Should_Rollback_On_Failure()
    {
        // ARRANGE: Create Aspire application for testing using Microsoft documented pattern
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        var connectionString = await app.GetConnectionStringAsync("database");
        Assert.NotNull(connectionString);
        
        var transactionService = new PostgreSqlTransactionResilienceService();
        
        // ACT & ASSERT: Should implement proper transaction rollback
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await transactionService.ExecuteTransactionWithRollbackAsync(connectionString, async (connection, transaction) =>
            {
                await connection.ExecuteAsync("CREATE TABLE test_rollback (id INT)", transaction);
                await connection.ExecuteAsync("INSERT INTO test_rollback VALUES (1)", transaction);
                
                throw new InvalidOperationException("Simulated transaction failure");
            });
        });
        
        // Verify table was NOT created (rollback successful)
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        var tableExists = await connection.QuerySingleOrDefaultAsync<bool>(
            "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'test_rollback')");
        
        Assert.False(tableExists);
        _logger.LogInformation("TDD GREEN: Transaction rollback working correctly through Aspire orchestration");
    }

    [Fact(DisplayName = "TDD GREEN: Aspire PostgreSQL Performance - Should meet reasonable response times")]
    public async Task AspirePostgreSQL_Performance_Should_Meet_Reasonable_Standards()
    {
        // ARRANGE: Create Aspire application for testing using Microsoft documented pattern
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        var connectionString = await app.GetConnectionStringAsync("database");
        Assert.NotNull(connectionString);
        
        var performanceService = new PostgreSqlPerformanceValidationService();
        
        // ACT & ASSERT: Should meet reasonable performance requirements through Aspire
        const int operationsCount = 50;
        var tasks = Enumerable.Range(0, operationsCount)
            .Select(async i => await performanceService.ExecuteQueryWithMetricsAsync(
                connectionString,
                "SELECT 1"))
            .ToArray();
            
        var results = await Task.WhenAll(tasks);
        
        // Reasonable requirements for Aspire orchestrated services
        Assert.All(results, result => 
            Assert.True(result.ResponseTime < TimeSpan.FromSeconds(2), 
                $"Aspire orchestrated services should have reasonable response times. Actual: {result.ResponseTime.TotalMilliseconds}ms"));
        
        _logger.LogInformation("TDD GREEN: Performance validation through Aspire with {AverageResponseTime}ms average", 
            results.Average(r => r.ResponseTime.TotalMilliseconds));
    }

    [Fact(DisplayName = "TDD GREEN: Aspire PostgreSQL Health Monitoring - Should provide comprehensive health checks")]
    public async Task AspirePostgreSQL_Health_Monitoring_Should_Provide_Comprehensive_Status()
    {
        // ARRANGE: Create Aspire application for testing using Microsoft documented pattern
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        var connectionString = await app.GetConnectionStringAsync("database");
        Assert.NotNull(connectionString);
        
        var healthService = new PostgreSqlHealthMonitoringService();
        
        // ACT & ASSERT: Should implement comprehensive health monitoring through Aspire
        var healthReport = await healthService.GetComprehensiveHealthReportAsync(connectionString);
        
        Assert.NotNull(healthReport);
        Assert.True(healthReport.IsHealthy);
        Assert.True(healthReport.ResponseTime < TimeSpan.FromSeconds(1));
        Assert.True(healthReport.ConnectionCount > 0);
        Assert.True(healthReport.AvailableConnections > 0);
        Assert.True(healthReport.DatabaseSize > 0);
        Assert.True(healthReport.LastBackupTimestamp > DateTime.UtcNow.AddHours(-24));
        
        _logger.LogInformation("TDD GREEN: Health monitoring through Aspire - ResponseTime: {ResponseTime}ms, Healthy: {IsHealthy}", 
            healthReport.ResponseTime.TotalMilliseconds, healthReport.IsHealthy);
    }

    [Fact(DisplayName = "TDD GREEN: Aspire PostgreSQL Security - Should enforce authentication standards")]
    public async Task AspirePostgreSQL_Security_Should_Enforce_Authentication_Standards()
    {
        // ARRANGE: Create Aspire application for testing using Microsoft documented pattern
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        var connectionString = await app.GetConnectionStringAsync("database");
        Assert.NotNull(connectionString);
        
        var securityService = new PostgreSqlSecurityValidationService();
        
        // ACT & ASSERT: Should implement security validation through Aspire
        var securityReport = await securityService.ValidateMedicalGradeSecurityAsync(connectionString);
        
        Assert.Contains("scram-sha-256", securityReport.AuthenticationMethod);
        Assert.False(securityReport.WeakPasswordsDetected);
        Assert.Equal(0, securityReport.UnauthorizedAccessAttempts);
        Assert.True(securityReport.AuditLoggingEnabled);
        Assert.True(securityReport.BackupEncryptionEnabled);
        
        _logger.LogInformation("TDD GREEN: Security validation through Aspire - Auth: {AuthMethod}", 
            securityReport.AuthenticationMethod);
    }

    [Fact(DisplayName = "TDD GREEN: Aspire Configuration Injection - Should provide correct connection strings")]
    public async Task AspirePostgreSQL_Configuration_Should_Provide_Valid_Connection_Strings()
    {
        // ARRANGE: Create Aspire application for testing using Microsoft documented pattern
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        // ACT: Get connection string through Aspire service discovery
        var connectionString = await app.GetConnectionStringAsync("database");
        
        // ASSERT: Should provide valid connection string
        Assert.NotNull(connectionString);
        Assert.Contains("postgres", connectionString.ToLower());
        Assert.Contains("localhost", connectionString.ToLower());
        Assert.Contains("database", connectionString.ToLower());
        
        // Verify connection works
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        var result = await connection.QuerySingleAsync<int>("SELECT 1");
        Assert.Equal(1, result);
        
        _logger.LogInformation("TDD GREEN: Aspire configuration injection providing valid connection strings");
    }
}