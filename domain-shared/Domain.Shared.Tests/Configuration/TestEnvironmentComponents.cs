using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit.Abstractions;

namespace InternationalCenter.Tests.Shared.Configuration;

/// <summary>
/// Provides standardized test environment information
/// Centralizes environment detection and system information for consistent test behavior
/// </summary>
public class TestEnvironmentInfo
{
    private readonly IConfiguration _configuration;
    private readonly Lazy<Dictionary<string, object>> _environmentData;

    public TestEnvironmentInfo(IConfiguration configuration)
    {
        _configuration = configuration;
        _environmentData = new Lazy<Dictionary<string, object>>(CollectEnvironmentData);
    }

    /// <summary>
    /// Gets whether we're running in CI/CD environment
    /// </summary>
    public bool IsContinuousIntegration => 
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_PIPELINES")) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JENKINS_URL"));

    /// <summary>
    /// Gets whether we're running in a container environment
    /// </summary>
    public bool IsContainer => 
        File.Exists("/.dockerenv") || 
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST"));

    /// <summary>
    /// Gets the current operating system
    /// </summary>
    public string OperatingSystem => RuntimeInformation.OSDescription;

    /// <summary>
    /// Gets the .NET runtime version
    /// </summary>
    public string RuntimeVersion => RuntimeInformation.FrameworkDescription;

    /// <summary>
    /// Gets the current machine name (sanitized for CI)
    /// </summary>
    public string MachineName => IsContinuousIntegration ? "ci-runner" : Environment.MachineName;

    /// <summary>
    /// Gets the test execution timestamp
    /// </summary>
    public DateTime ExecutionTimestamp { get; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the test assembly information
    /// </summary>
    public AssemblyInfo TestAssembly { get; }

    /// <summary>
    /// Gets all environment data as a dictionary
    /// </summary>
    public IReadOnlyDictionary<string, object> EnvironmentData => _environmentData.Value;

    public TestEnvironmentInfo(IConfiguration configuration, Assembly? testAssembly = null)
    {
        _configuration = configuration;
        _environmentData = new Lazy<Dictionary<string, object>>(CollectEnvironmentData);
        TestAssembly = new AssemblyInfo(testAssembly ?? Assembly.GetCallingAssembly());
    }

    /// <summary>
    /// Gets standardized test tags based on environment
    /// </summary>
    public IEnumerable<string> GetTestTags()
    {
        var tags = new List<string>();

        if (IsContinuousIntegration) tags.Add("ci");
        if (IsContainer) tags.Add("container");
        
        tags.Add(RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant());
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) tags.Add("windows");
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) tags.Add("linux");
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) tags.Add("macos");

        return tags;
    }

    private Dictionary<string, object> CollectEnvironmentData()
    {
        return new Dictionary<string, object>
        {
            { "IsContinuousIntegration", IsContinuousIntegration },
            { "IsContainer", IsContainer },
            { "OperatingSystem", OperatingSystem },
            { "RuntimeVersion", RuntimeVersion },
            { "MachineName", MachineName },
            { "ExecutionTimestamp", ExecutionTimestamp },
            { "ProcessorCount", Environment.ProcessorCount },
            { "WorkingMemoryMB", GC.GetTotalMemory(false) / (1024 * 1024) },
            { "TestTags", GetTestTags().ToArray() }
        };
    }

    public class AssemblyInfo
    {
        public string Name { get; }
        public Version Version { get; }
        public string Location { get; }

        public AssemblyInfo(Assembly assembly)
        {
            Name = assembly.GetName().Name ?? "Unknown";
            Version = assembly.GetName().Version ?? new Version();
            Location = assembly.Location;
        }
    }
}

/// <summary>
/// Manages test data lifecycle with standardized patterns
/// Provides consistent test data creation, cleanup, and validation
/// </summary>
public class TestDataManager
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TestDataManager> _logger;
    private readonly TestDataConfiguration _config;
    private readonly ConcurrentDictionary<string, TestDataSession> _activeSessions;

    public TestDataManager(IConfiguration configuration, ILogger<TestDataManager> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _config = StandardizedTestEnvironment.GetTestDataConfiguration();
        _activeSessions = new ConcurrentDictionary<string, TestDataSession>();
    }

    /// <summary>
    /// Creates a new test data session for a test class
    /// </summary>
    public TestDataSession CreateSession(string testClassName)
    {
        var sessionId = $"{testClassName}_{Guid.NewGuid():N}";
        var session = new TestDataSession(sessionId, _config, _logger);
        
        _activeSessions.TryAdd(sessionId, session);
        _logger.LogDebug("Created test data session {SessionId} for {TestClassName}", sessionId, testClassName);
        
        return session;
    }

    /// <summary>
    /// Disposes a test data session and cleans up resources
    /// </summary>
    public async Task DisposeSessionAsync(string sessionId)
    {
        if (_activeSessions.TryRemove(sessionId, out var session))
        {
            await session.DisposeAsync();
            _logger.LogDebug("Disposed test data session {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// Gets statistics about active test data sessions
    /// </summary>
    public TestDataStatistics GetStatistics()
    {
        return new TestDataStatistics
        {
            ActiveSessions = _activeSessions.Count,
            TotalEntitiesCreated = _activeSessions.Values.Sum(s => s.EntitiesCreated),
            TotalCleanupOperations = _activeSessions.Values.Sum(s => s.CleanupOperations)
        };
    }
}

/// <summary>
/// Represents a test data session for a specific test class
/// </summary>
public class TestDataSession : IAsyncDisposable
{
    public string SessionId { get; }
    public int EntitiesCreated { get; private set; }
    public int CleanupOperations { get; private set; }
    
    private readonly TestDataConfiguration _config;
    private readonly ILogger _logger;
    private readonly List<Func<Task>> _cleanupActions;
    private bool _disposed = false;

    internal TestDataSession(string sessionId, TestDataConfiguration config, ILogger logger)
    {
        SessionId = sessionId;
        _config = config;
        _logger = logger;
        _cleanupActions = new List<Func<Task>>();
    }

    /// <summary>
    /// Registers an entity for cleanup when the session ends
    /// </summary>
    public void RegisterEntity<T>(T entity, Func<Task> cleanupAction)
    {
        EntitiesCreated++;
        _cleanupActions.Add(cleanupAction);
        
        if (EntitiesCreated > _config.MaxTestEntities)
        {
            _logger.LogWarning("Test session {SessionId} exceeded maximum entities limit ({MaxEntities})", 
                SessionId, _config.MaxTestEntities);
        }
    }

    /// <summary>
    /// Performs cleanup of all registered entities
    /// </summary>
    public async Task CleanupAsync()
    {
        foreach (var cleanupAction in _cleanupActions)
        {
            try
            {
                await cleanupAction();
                CleanupOperations++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup entity in session {SessionId}", SessionId);
            }
        }
        
        _cleanupActions.Clear();
        _logger.LogDebug("Completed cleanup for session {SessionId}: {CleanupCount} operations", 
            SessionId, CleanupOperations);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await CleanupAsync();
            _disposed = true;
        }
    }
}

/// <summary>
/// Statistics about test data management
/// </summary>
public class TestDataStatistics
{
    public int ActiveSessions { get; init; }
    public int TotalEntitiesCreated { get; init; }
    public int TotalCleanupOperations { get; init; }
}

/// <summary>
/// Tracks performance metrics for test operations
/// Provides consistent performance monitoring across test projects
/// </summary>
public class TestPerformanceTracker
{
    private readonly ILogger<TestPerformanceTracker> _logger;
    private readonly PerformanceTestConfiguration _config;
    private readonly ConcurrentDictionary<string, PerformanceMetric> _metrics;

    public TestPerformanceTracker(ILogger<TestPerformanceTracker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _config = StandardizedTestEnvironment.GetPerformanceConfiguration();
        _metrics = new ConcurrentDictionary<string, PerformanceMetric>();
    }

    /// <summary>
    /// Tracks the execution time of a test operation
    /// </summary>
    public async Task<T> TrackAsync<T>(string operationName, Func<Task<T>> operation, TimeSpan? maxDuration = null)
    {
        if (!_config.EnablePerformanceValidation)
        {
            return await operation();
        }

        var stopwatch = Stopwatch.StartNew();
        var startMemory = GC.GetTotalMemory(false);

        try
        {
            var result = await operation();
            stopwatch.Stop();
            
            var endMemory = GC.GetTotalMemory(false);
            var metric = new PerformanceMetric
            {
                OperationName = operationName,
                Duration = stopwatch.Elapsed,
                MemoryAllocated = endMemory - startMemory,
                Timestamp = DateTime.UtcNow,
                Success = true
            };

            _metrics.TryAdd($"{operationName}_{DateTime.UtcNow:HHmmssfff}", metric);
            
            // Validate performance if threshold is specified
            var threshold = maxDuration ?? GetDefaultThreshold(operationName);
            if (threshold > TimeSpan.Zero && stopwatch.Elapsed > threshold)
            {
                _logger.LogWarning("Performance threshold exceeded for {OperationName}: {Duration}ms > {Threshold}ms",
                    operationName, stopwatch.ElapsedMilliseconds, threshold.TotalMilliseconds);
            }

            if (_config.CollectDetailedMetrics)
            {
                _logger.LogDebug("Performance metric for {OperationName}: {Duration}ms, Memory: {Memory}MB",
                    operationName, stopwatch.ElapsedMilliseconds, (endMemory - startMemory) / (1024 * 1024));
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            var metric = new PerformanceMetric
            {
                OperationName = operationName,
                Duration = stopwatch.Elapsed,
                Timestamp = DateTime.UtcNow,
                Success = false,
                ErrorMessage = ex.Message
            };

            _metrics.TryAdd($"{operationName}_{DateTime.UtcNow:HHmmssfff}", metric);
            throw;
        }
    }

    /// <summary>
    /// Gets performance statistics for all tracked operations
    /// </summary>
    public PerformanceStatistics GetStatistics()
    {
        var metrics = _metrics.Values.ToList();
        
        return new PerformanceStatistics
        {
            TotalOperations = metrics.Count,
            SuccessfulOperations = metrics.Count(m => m.Success),
            AverageDuration = metrics.Any() ? TimeSpan.FromTicks((long)metrics.Average(m => m.Duration.Ticks)) : TimeSpan.Zero,
            MaxDuration = metrics.Any() ? metrics.Max(m => m.Duration) : TimeSpan.Zero,
            TotalMemoryAllocated = metrics.Sum(m => m.MemoryAllocated)
        };
    }

    private TimeSpan GetDefaultThreshold(string operationName)
    {
        return operationName.ToLowerInvariant() switch
        {
            var name when name.Contains("unit") => _config.UnitTestMaxDuration,
            var name when name.Contains("integration") => _config.IntegrationTestMaxDuration,
            var name when name.Contains("database") => _config.DatabaseOperationMaxDuration,
            _ => TimeSpan.Zero
        };
    }
}

/// <summary>
/// Performance metric for a single operation
/// </summary>
public class PerformanceMetric
{
    public string OperationName { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; }
    public long MemoryAllocated { get; init; }
    public DateTime Timestamp { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Aggregated performance statistics
/// </summary>
public class PerformanceStatistics
{
    public int TotalOperations { get; init; }
    public int SuccessfulOperations { get; init; }
    public TimeSpan AverageDuration { get; init; }
    public TimeSpan MaxDuration { get; init; }
    public long TotalMemoryAllocated { get; init; }
    
    public double SuccessRate => TotalOperations > 0 ? (double)SuccessfulOperations / TotalOperations : 0;
}

/// <summary>
/// Test-specific logger provider that integrates with xUnit test output
/// Provides consistent logging across test projects with test context
/// </summary>
public class TestLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, TestLogger> _loggers;

    public TestLoggerProvider()
    {
        _loggers = new ConcurrentDictionary<string, TestLogger>();
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new TestLogger(name));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}

/// <summary>
/// Test-specific logger implementation
/// </summary>
public class TestLogger : ILogger
{
    private readonly string _categoryName;

    public TestLogger(string categoryName)
    {
        _categoryName = categoryName;
    }

    public IDisposable BeginScope<TState>(TState state) => new TestLoggerScope();

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        var timestamp = DateTime.UtcNow.ToString("HH:mm:ss.fff");
        
        // In a real implementation, this could be routed to test output
        // For now, we'll use Console for consistency
        Console.WriteLine($"[{timestamp}] [{logLevel}] {_categoryName}: {message}");
        
        if (exception != null)
        {
            Console.WriteLine($"Exception: {exception}");
        }
    }

    private class TestLoggerScope : IDisposable
    {
        public void Dispose() { }
    }
}