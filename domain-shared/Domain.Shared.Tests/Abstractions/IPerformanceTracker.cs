using Microsoft.Extensions.Logging;

namespace InternationalCenter.Shared.Tests.Abstractions;

/// <summary>
/// Contract for test performance tracking with dependency inversion
/// Provides consistent performance monitoring, validation, and reporting across all test domains
/// Medical-grade performance tracking ensuring test operations meet performance contracts
/// </summary>
public interface IPerformanceTracker : IDisposable
{
    /// <summary>
    /// Tracks the execution time and performance metrics of a test operation with return value
    /// Contract: Must validate performance against specified thresholds and log violations
    /// </summary>
    Task<T> TrackAsync<T>(
        string operationName,
        Func<Task<T>> operation,
        TimeSpan? maxDuration = null,
        PerformanceThreshold? threshold = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Tracks the execution time and performance metrics of a test operation without return value
    /// </summary>
    Task TrackAsync(
        string operationName,
        Func<Task> operation,
        TimeSpan? maxDuration = null,
        PerformanceThreshold? threshold = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Tracks a synchronous operation
    /// Contract: Should be avoided for async operations to prevent thread blocking
    /// </summary>
    T Track<T>(
        string operationName,
        Func<T> operation,
        TimeSpan? maxDuration = null,
        PerformanceThreshold? threshold = null);
    
    /// <summary>
    /// Tracks a synchronous operation without return value
    /// </summary>
    void Track(
        string operationName,
        Action operation,
        TimeSpan? maxDuration = null,
        PerformanceThreshold? threshold = null);
    
    /// <summary>
    /// Validates that an operation meets performance requirements
    /// Contract: Must throw PerformanceContractViolationException for violations
    /// </summary>
    Task ValidatePerformanceAsync(
        string operationName,
        TimeSpan actualDuration,
        long memoryUsed,
        PerformanceThreshold threshold);
    
    /// <summary>
    /// Gets performance statistics for all tracked operations
    /// </summary>
    IPerformanceStatistics GetStatistics();
    
    /// <summary>
    /// Gets performance statistics for a specific operation
    /// </summary>
    IOperationPerformanceStatistics? GetOperationStatistics(string operationName);
    
    /// <summary>
    /// Gets performance metrics for all operations matching a pattern
    /// </summary>
    IEnumerable<IOperationPerformanceStatistics> GetOperationStatistics(string operationNamePattern);
    
    /// <summary>
    /// Resets all performance tracking data
    /// Contract: Should be used between test runs to prevent data pollution
    /// </summary>
    void Reset();
}

/// <summary>
/// Contract for performance threshold configuration
/// Defines acceptable performance limits for test operations
/// </summary>
public interface IPerformanceThreshold
{
    /// <summary>
    /// Gets the maximum acceptable duration for the operation
    /// </summary>
    TimeSpan MaxDuration { get; }
    
    /// <summary>
    /// Gets the maximum acceptable memory allocation for the operation
    /// </summary>
    long MaxMemoryAllocation { get; }
    
    /// <summary>
    /// Gets the maximum acceptable CPU usage percentage
    /// </summary>
    double MaxCpuUsagePercent { get; }
    
    /// <summary>
    /// Gets whether the threshold validation is strict (throws exceptions) or warning-only
    /// </summary>
    bool IsStrict { get; }
    
    /// <summary>
    /// Gets the performance threshold category (unit test, integration test, etc.)
    /// </summary>
    PerformanceCategory Category { get; }
}

/// <summary>
/// Performance threshold categories for different test types
/// </summary>
public enum PerformanceCategory
{
    /// <summary>
    /// Unit test performance thresholds (fast, 5-second timeout)
    /// </summary>
    UnitTest,
    
    /// <summary>
    /// Integration test performance thresholds (moderate, 30-second timeout)
    /// </summary>
    IntegrationTest,
    
    /// <summary>
    /// End-to-end test performance thresholds (slower, 30-second timeout)
    /// </summary>
    EndToEndTest,
    
    /// <summary>
    /// Load test performance thresholds (extended, 5-minute timeout)
    /// </summary>
    LoadTest,
    
    /// <summary>
    /// Database operation performance thresholds
    /// </summary>
    DatabaseOperation,
    
    /// <summary>
    /// API call performance thresholds
    /// </summary>
    ApiCall,
    
    /// <summary>
    /// Custom performance thresholds
    /// </summary>
    Custom
}

/// <summary>
/// Contract for overall performance statistics
/// </summary>
public interface IPerformanceStatistics
{
    /// <summary>
    /// Gets the total number of operations tracked
    /// </summary>
    int TotalOperations { get; }
    
    /// <summary>
    /// Gets the number of successful operations
    /// </summary>
    int SuccessfulOperations { get; }
    
    /// <summary>
    /// Gets the number of failed operations
    /// </summary>
    int FailedOperations { get; }
    
    /// <summary>
    /// Gets the number of operations that exceeded performance thresholds
    /// </summary>
    int ThresholdViolations { get; }
    
    /// <summary>
    /// Gets the average duration across all operations
    /// </summary>
    TimeSpan AverageDuration { get; }
    
    /// <summary>
    /// Gets the maximum duration recorded
    /// </summary>
    TimeSpan MaxDuration { get; }
    
    /// <summary>
    /// Gets the minimum duration recorded
    /// </summary>
    TimeSpan MinDuration { get; }
    
    /// <summary>
    /// Gets the total memory allocated across all operations
    /// </summary>
    long TotalMemoryAllocated { get; }
    
    /// <summary>
    /// Gets the success rate as a percentage
    /// </summary>
    double SuccessRate { get; }
    
    /// <summary>
    /// Gets the performance compliance rate as a percentage
    /// </summary>
    double ComplianceRate { get; }
    
    /// <summary>
    /// Gets the statistics collection timestamp
    /// </summary>
    DateTime CollectedAt { get; }
}

/// <summary>
/// Contract for operation-specific performance statistics
/// </summary>
public interface IOperationPerformanceStatistics
{
    /// <summary>
    /// Gets the operation name
    /// </summary>
    string OperationName { get; }
    
    /// <summary>
    /// Gets the number of times this operation was executed
    /// </summary>
    int ExecutionCount { get; }
    
    /// <summary>
    /// Gets the number of successful executions
    /// </summary>
    int SuccessfulExecutions { get; }
    
    /// <summary>
    /// Gets the average execution duration
    /// </summary>
    TimeSpan AverageDuration { get; }
    
    /// <summary>
    /// Gets the fastest execution duration
    /// </summary>
    TimeSpan FastestDuration { get; }
    
    /// <summary>
    /// Gets the slowest execution duration
    /// </summary>
    TimeSpan SlowestDuration { get; }
    
    /// <summary>
    /// Gets the 95th percentile execution duration
    /// </summary>
    TimeSpan Percentile95Duration { get; }
    
    /// <summary>
    /// Gets the total memory allocated for this operation
    /// </summary>
    long TotalMemoryAllocated { get; }
    
    /// <summary>
    /// Gets the number of threshold violations for this operation
    /// </summary>
    int ThresholdViolations { get; }
    
    /// <summary>
    /// Gets the success rate for this operation
    /// </summary>
    double SuccessRate { get; }
}

/// <summary>
/// Concrete implementation of performance threshold
/// </summary>
public record PerformanceThreshold : IPerformanceThreshold
{
    public TimeSpan MaxDuration { get; init; }
    public long MaxMemoryAllocation { get; init; }
    public double MaxCpuUsagePercent { get; init; }
    public bool IsStrict { get; init; }
    public PerformanceCategory Category { get; init; }
    
    /// <summary>
    /// Creates a unit test performance threshold (fast, strict)
    /// </summary>
    public static PerformanceThreshold UnitTest(TimeSpan? maxDuration = null) => new()
    {
        MaxDuration = maxDuration ?? TimeSpan.FromMilliseconds(100),
        MaxMemoryAllocation = 50 * 1024 * 1024, // 50MB
        MaxCpuUsagePercent = 80,
        IsStrict = true,
        Category = PerformanceCategory.UnitTest
    };
    
    /// <summary>
    /// Creates an integration test performance threshold (moderate, strict)
    /// </summary>
    public static PerformanceThreshold IntegrationTest(TimeSpan? maxDuration = null) => new()
    {
        MaxDuration = maxDuration ?? TimeSpan.FromSeconds(5),
        MaxMemoryAllocation = 200 * 1024 * 1024, // 200MB
        MaxCpuUsagePercent = 90,
        IsStrict = true,
        Category = PerformanceCategory.IntegrationTest
    };
    
    /// <summary>
    /// Creates an end-to-end test performance threshold (slower, lenient)
    /// </summary>
    public static PerformanceThreshold EndToEndTest(TimeSpan? maxDuration = null) => new()
    {
        MaxDuration = maxDuration ?? TimeSpan.FromSeconds(10),
        MaxMemoryAllocation = 500 * 1024 * 1024, // 500MB
        MaxCpuUsagePercent = 95,
        IsStrict = false,
        Category = PerformanceCategory.EndToEndTest
    };
}