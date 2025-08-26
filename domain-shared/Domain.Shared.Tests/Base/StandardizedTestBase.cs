using InternationalCenter.Tests.Shared.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace InternationalCenter.Tests.Shared.Base;

/// <summary>
/// Standardized base class for all test types with consistent configuration management
/// Eliminates configuration duplication and provides standardized test infrastructure
/// Medical-grade test base with comprehensive configuration, logging, and performance tracking
/// </summary>
/// <typeparam name="TTestClass">The concrete test class type for categorization</typeparam>
public abstract class StandardizedTestBase<TTestClass> : IAsyncLifetime, IDisposable
    where TTestClass : class
{
    protected readonly ITestOutputHelper Output;
    protected readonly TestEnvironmentScope TestScope;
    protected readonly IConfiguration Configuration;
    protected readonly ILogger<TTestClass> Logger;
    protected readonly TestPerformanceTracker PerformanceTracker;
    protected readonly TestDataManager DataManager;
    protected readonly TestEnvironmentInfo EnvironmentInfo;
    
    private readonly TestDataSession _dataSession;
    private bool _disposed = false;

    protected StandardizedTestBase(ITestOutputHelper output)
    {
        Output = output ?? throw new ArgumentNullException(nameof(output));
        
        // Validate test environment on initialization
        StandardizedTestEnvironment.ValidateTestEnvironment();
        
        // Create standardized test scope
        TestScope = StandardizedTestEnvironment.CreateTestScope(output, typeof(TTestClass).Name);
        
        // Get standardized services
        Configuration = TestScope.GetRequiredService<IConfiguration>();
        Logger = TestScope.GetRequiredService<ILogger<TTestClass>>();
        PerformanceTracker = TestScope.GetRequiredService<TestPerformanceTracker>();
        DataManager = TestScope.GetRequiredService<TestDataManager>();
        EnvironmentInfo = TestScope.GetRequiredService<TestEnvironmentInfo>();
        
        // Create data session for this test class
        _dataSession = DataManager.CreateSession(typeof(TTestClass).Name);
        
        Logger.LogInformation("Initialized standardized test base for {TestClass} in {Environment}", 
            typeof(TTestClass).Name, GetTestEnvironmentDescription());
    }

    /// <summary>
    /// Gets standardized timeout for the current test type
    /// Automatically selects appropriate timeout based on test class naming conventions
    /// </summary>
    protected virtual TimeSpan GetTestTimeout()
    {
        var timeouts = StandardizedTestEnvironment.GetTestTimeouts();
        var testClassName = typeof(TTestClass).Name.ToLowerInvariant();
        
        return testClassName switch
        {
            var name when name.Contains("unit") => TimeSpan.FromMilliseconds(timeouts.UnitTestTimeout),
            var name when name.Contains("integration") => TimeSpan.FromMilliseconds(timeouts.IntegrationTestTimeout),
            var name when name.Contains("endtoend") || name.Contains("e2e") => TimeSpan.FromMilliseconds(timeouts.EndToEndTestTimeout),
            var name when name.Contains("load") || name.Contains("performance") => TimeSpan.FromMilliseconds(timeouts.LoadTestTimeout),
            _ => TimeSpan.FromMilliseconds(timeouts.IntegrationTestTimeout) // Default to integration test timeout
        };
    }

    /// <summary>
    /// Executes an operation with standardized performance tracking
    /// Automatically validates performance against test type thresholds
    /// </summary>
    protected async Task<T> ExecuteWithPerformanceTrackingAsync<T>(
        string operationName,
        Func<Task<T>> operation,
        TimeSpan? maxDuration = null)
    {
        var effectiveMaxDuration = maxDuration ?? GetDefaultPerformanceThreshold(operationName);
        return await PerformanceTracker.TrackAsync(operationName, operation, effectiveMaxDuration);
    }

    /// <summary>
    /// Executes an operation with standardized performance tracking (no return value)
    /// </summary>
    protected async Task ExecuteWithPerformanceTrackingAsync(
        string operationName,
        Func<Task> operation,
        TimeSpan? maxDuration = null)
    {
        await ExecuteWithPerformanceTrackingAsync(operationName, async () =>
        {
            await operation();
            return true;
        }, maxDuration);
    }

    /// <summary>
    /// Registers test data for automatic cleanup
    /// Ensures consistent test data isolation across test runs
    /// </summary>
    protected void RegisterTestData<T>(T entity, Func<Task> cleanupAction)
    {
        _dataSession.RegisterEntity(entity, cleanupAction);
        Logger.LogDebug("Registered test data entity of type {EntityType} for cleanup", typeof(T).Name);
    }

    /// <summary>
    /// Gets standardized test configuration section
    /// Provides type-safe access to configuration with fallback defaults
    /// </summary>
    protected T GetTestConfiguration<T>(string sectionName, T defaultValue = default!)
        where T : class, new()
    {
        var section = Configuration.GetSection(sectionName);
        
        if (!section.Exists())
        {
            Logger.LogWarning("Configuration section {SectionName} not found, using default", sectionName);
            return defaultValue ?? new T();
        }
        
        var config = section.Get<T>();
        if (config == null)
        {
            Logger.LogWarning("Failed to bind configuration section {SectionName}, using default", sectionName);
            return defaultValue ?? new T();
        }
        
        return config;
    }

    /// <summary>
    /// Logs standardized test completion information
    /// Provides consistent test result logging with performance metrics
    /// </summary>
    protected void LogTestCompletion(string testName, bool success, TimeSpan? duration = null)
    {
        var status = success ? "PASSED" : "FAILED";
        var durationText = duration?.TotalMilliseconds.ToString("F0") ?? "N/A";
        var environmentTags = string.Join(", ", EnvironmentInfo.GetTestTags());
        
        Logger.LogInformation("✅ TEST {Status}: {TestName} in {Duration}ms [{Tags}]", 
            status, testName, durationText, environmentTags);
        
        // Log performance statistics if available
        var perfStats = PerformanceTracker.GetStatistics();
        if (perfStats.TotalOperations > 0)
        {
            Logger.LogInformation("📊 PERFORMANCE STATS: {Operations} ops, {AvgDuration}ms avg, {SuccessRate:P1} success rate",
                perfStats.TotalOperations, perfStats.AverageDuration.TotalMilliseconds, perfStats.SuccessRate);
        }
    }

    /// <summary>
    /// Standardized test initialization - override in derived classes for custom setup
    /// </summary>
    public virtual async Task InitializeAsync()
    {
        await Task.CompletedTask;
        Logger.LogDebug("Completed standardized test initialization for {TestClass}", typeof(TTestClass).Name);
    }

    /// <summary>
    /// Standardized test cleanup - override in derived classes for custom cleanup
    /// </summary>
    public virtual async Task DisposeAsync()
    {
        try
        {
            // Cleanup test data
            if (_dataSession != null)
            {
                await _dataSession.CleanupAsync();
                await DataManager.DisposeSessionAsync(_dataSession.SessionId);
            }

            // Log final statistics
            var dataStats = DataManager.GetStatistics();
            var perfStats = PerformanceTracker.GetStatistics();
            
            Logger.LogInformation("🧹 TEST CLEANUP: {EntitiesCreated} entities created, {CleanupOps} cleanup operations, {PerfOps} performance operations",
                dataStats.TotalEntitiesCreated, dataStats.TotalCleanupOperations, perfStats.TotalOperations);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during test cleanup for {TestClass}", typeof(TTestClass).Name);
        }
        finally
        {
            Dispose();
        }
    }

    /// <summary>
    /// IDisposable implementation for synchronous cleanup
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            TestScope?.Dispose();
            _disposed = true;
        }
    }

    /// <summary>
    /// Gets environment description for logging
    /// </summary>
    private string GetTestEnvironmentDescription()
    {
        var tags = string.Join(", ", EnvironmentInfo.GetTestTags());
        return $"{EnvironmentInfo.OperatingSystem} [{tags}]";
    }

    /// <summary>
    /// Gets default performance threshold based on operation name and test type
    /// </summary>
    private TimeSpan GetDefaultPerformanceThreshold(string operationName)
    {
        var perfConfig = StandardizedTestEnvironment.GetPerformanceConfiguration();
        var testClassName = typeof(TTestClass).Name.ToLowerInvariant();
        
        // Database operations have specific thresholds
        if (operationName.ToLowerInvariant().Contains("database"))
        {
            return perfConfig.DatabaseOperationMaxDuration;
        }
        
        // Test-type specific thresholds
        return testClassName switch
        {
            var name when name.Contains("unit") => perfConfig.UnitTestMaxDuration,
            var name when name.Contains("integration") => perfConfig.IntegrationTestMaxDuration,
            _ => perfConfig.IntegrationTestMaxDuration
        };
    }
}

/// <summary>
/// Standardized base class specifically for unit tests
/// Provides unit test specific configuration and validation
/// </summary>
/// <typeparam name="TTestClass">The unit test class type</typeparam>
public abstract class StandardizedUnitTestBase<TTestClass> : StandardizedTestBase<TTestClass>
    where TTestClass : class
{
    protected StandardizedUnitTestBase(ITestOutputHelper output) : base(output)
    {
        // Unit tests should not take long to execute
        var timeouts = StandardizedTestEnvironment.GetTestTimeouts();
        if (timeouts.UnitTestTimeout > 10000) // More than 10 seconds is too long for unit tests
        {
            Logger.LogWarning("Unit test timeout is configured too high: {Timeout}ms", timeouts.UnitTestTimeout);
        }
    }

    protected override TimeSpan GetTestTimeout()
    {
        var timeouts = StandardizedTestEnvironment.GetTestTimeouts();
        return TimeSpan.FromMilliseconds(timeouts.UnitTestTimeout);
    }
}

/// <summary>
/// Standardized base class specifically for integration tests
/// Provides integration test specific configuration and infrastructure setup
/// </summary>
/// <typeparam name="TTestClass">The integration test class type</typeparam>
public abstract class StandardizedIntegrationTestBase<TTestClass> : StandardizedTestBase<TTestClass>
    where TTestClass : class
{
    protected readonly AspireTestConfiguration AspireConfig;

    protected StandardizedIntegrationTestBase(ITestOutputHelper output) : base(output)
    {
        AspireConfig = StandardizedTestEnvironment.GetAspireConfiguration();
        
        // Log integration test specific configuration
        Logger.LogDebug("Integration test configured with Aspire timeout: {Timeout}s, Health checks: {HealthChecks}",
            AspireConfig.StartTimeout.TotalSeconds, AspireConfig.WaitForHealthChecks);
    }

    protected override TimeSpan GetTestTimeout()
    {
        var timeouts = StandardizedTestEnvironment.GetTestTimeouts();
        return TimeSpan.FromMilliseconds(timeouts.IntegrationTestTimeout);
    }

    /// <summary>
    /// Waits for Aspire application to be ready with standardized retry logic
    /// </summary>
    protected async Task WaitForAspireReadinessAsync(Func<Task<bool>> readinessCheck)
    {
        var attempts = 0;
        var maxAttempts = AspireConfig.RetryAttempts;

        while (attempts < maxAttempts)
        {
            try
            {
                if (await readinessCheck())
                {
                    Logger.LogDebug("Aspire application ready after {Attempts} attempts", attempts + 1);
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Readiness check failed on attempt {Attempt}", attempts + 1);
            }

            attempts++;
            if (attempts < maxAttempts)
            {
                await Task.Delay(AspireConfig.RetryDelay);
            }
        }

        throw new InvalidOperationException($"Aspire application failed to become ready after {maxAttempts} attempts");
    }
}