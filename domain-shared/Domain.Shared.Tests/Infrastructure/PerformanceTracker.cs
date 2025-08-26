using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using InternationalCenter.Shared.Tests.Abstractions;

namespace InternationalCenter.Shared.Tests.Infrastructure;

/// <summary>
/// Concrete implementation of performance tracking for test operations
/// Provides detailed performance monitoring, validation, and statistical analysis
/// Medical-grade performance tracking with strict contract enforcement
/// </summary>
public class PerformanceTracker : IPerformanceTracker
{
    private readonly ILogger<PerformanceTracker> _logger;
    private readonly ConcurrentDictionary<string, OperationPerformanceStatistics> _operationStatistics;
    private readonly ConcurrentQueue<PerformanceRecord> _performanceRecords;
    private readonly object _lockObject = new();
    private bool _disposed;

    public PerformanceTracker(ILogger<PerformanceTracker> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _operationStatistics = new ConcurrentDictionary<string, OperationPerformanceStatistics>();
        _performanceRecords = new ConcurrentQueue<PerformanceRecord>();
    }

    /// <summary>
    /// Tracks the execution time and performance metrics of a test operation with return value
    /// Contract: Must validate performance against specified thresholds and log violations
    /// </summary>
    public async Task<T> TrackAsync<T>(
        string operationName,
        Func<Task<T>> operation,
        TimeSpan? maxDuration = null,
        PerformanceThreshold? threshold = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIfDisposed(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentNullException.ThrowIfNull(operation);

        var stopwatch = Stopwatch.StartNew();
        var initialMemory = GC.GetTotalMemory(false);
        var record = new PerformanceRecord
        {
            OperationName = operationName,
            StartTime = DateTime.UtcNow,
            ThreadId = Environment.CurrentManagedThreadId
        };

        try
        {
            _logger.LogDebug("Starting performance tracking for operation {OperationName}", operationName);

            var result = await operation();

            stopwatch.Stop();
            var finalMemory = GC.GetTotalMemory(false);
            var memoryUsed = Math.Max(0, finalMemory - initialMemory);

            record.Duration = stopwatch.Elapsed;
            record.MemoryAllocated = memoryUsed;
            record.IsSuccess = true;
            record.EndTime = DateTime.UtcNow;

            // Validate performance if threshold provided
            if (threshold != null)
            {
                await ValidatePerformanceAsync(operationName, record.Duration, memoryUsed, threshold);
            }
            else if (maxDuration.HasValue && record.Duration > maxDuration.Value)
            {
                var violation = new PerformanceContractViolationException(
                    operationName,
                    record.Duration,
                    maxDuration.Value);

                _logger.LogWarning("Performance threshold exceeded for {OperationName}: {Duration}ms > {MaxDuration}ms",
                    operationName, record.Duration.TotalMilliseconds, maxDuration.Value.TotalMilliseconds);

                throw violation;
            }

            RecordPerformance(record);
            UpdateOperationStatistics(record);

            _logger.LogDebug("Performance tracking completed for {OperationName}: {Duration}ms, {Memory}bytes",
                operationName, record.Duration.TotalMilliseconds, memoryUsed);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            record.Duration = stopwatch.Elapsed;
            record.IsSuccess = false;
            record.EndTime = DateTime.UtcNow;
            record.ErrorMessage = ex.Message;

            RecordPerformance(record);
            UpdateOperationStatistics(record);

            _logger.LogError(ex, "Operation {OperationName} failed after {Duration}ms",
                operationName, record.Duration.TotalMilliseconds);

            throw;
        }
    }

    /// <summary>
    /// Tracks the execution time and performance metrics of a test operation without return value
    /// </summary>
    public async Task TrackAsync(
        string operationName,
        Func<Task> operation,
        TimeSpan? maxDuration = null,
        PerformanceThreshold? threshold = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await TrackAsync<object?>(
            operationName,
            async () =>
            {
                await operation();
                return null;
            },
            maxDuration,
            threshold,
            cancellationToken);
    }

    /// <summary>
    /// Tracks a synchronous operation
    /// Contract: Should be avoided for async operations to prevent thread blocking
    /// </summary>
    public T Track<T>(
        string operationName,
        Func<T> operation,
        TimeSpan? maxDuration = null,
        PerformanceThreshold? threshold = null)
    {
        ObjectDisposedException.ThrowIfDisposed(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentNullException.ThrowIfNull(operation);

        var stopwatch = Stopwatch.StartNew();
        var initialMemory = GC.GetTotalMemory(false);
        var record = new PerformanceRecord
        {
            OperationName = operationName,
            StartTime = DateTime.UtcNow,
            ThreadId = Environment.CurrentManagedThreadId
        };

        try
        {
            _logger.LogDebug("Starting synchronous performance tracking for operation {OperationName}", operationName);

            var result = operation();

            stopwatch.Stop();
            var finalMemory = GC.GetTotalMemory(false);
            var memoryUsed = Math.Max(0, finalMemory - initialMemory);

            record.Duration = stopwatch.Elapsed;
            record.MemoryAllocated = memoryUsed;
            record.IsSuccess = true;
            record.EndTime = DateTime.UtcNow;

            // Validate performance if threshold provided
            if (threshold != null)
            {
                ValidatePerformanceAsync(operationName, record.Duration, memoryUsed, threshold)
                    .GetAwaiter()
                    .GetResult();
            }
            else if (maxDuration.HasValue && record.Duration > maxDuration.Value)
            {
                throw new PerformanceContractViolationException(
                    operationName,
                    record.Duration,
                    maxDuration.Value);
            }

            RecordPerformance(record);
            UpdateOperationStatistics(record);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            record.Duration = stopwatch.Elapsed;
            record.IsSuccess = false;
            record.EndTime = DateTime.UtcNow;
            record.ErrorMessage = ex.Message;

            RecordPerformance(record);
            UpdateOperationStatistics(record);

            _logger.LogError(ex, "Synchronous operation {OperationName} failed after {Duration}ms",
                operationName, record.Duration.TotalMilliseconds);

            throw;
        }
    }

    /// <summary>
    /// Tracks a synchronous operation without return value
    /// </summary>
    public void Track(
        string operationName,
        Action operation,
        TimeSpan? maxDuration = null,
        PerformanceThreshold? threshold = null)
    {
        ArgumentNullException.ThrowIfNull(operation);

        Track<object?>(
            operationName,
            () =>
            {
                operation();
                return null;
            },
            maxDuration,
            threshold);
    }

    /// <summary>
    /// Validates that an operation meets performance requirements
    /// Contract: Must throw PerformanceContractViolationException for violations
    /// </summary>
    public Task ValidatePerformanceAsync(
        string operationName,
        TimeSpan actualDuration,
        long memoryUsed,
        PerformanceThreshold threshold)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentNullException.ThrowIfNull(threshold);

        var violations = new List<string>();

        // Validate duration
        if (actualDuration > threshold.MaxDuration)
        {
            violations.Add($"Duration {actualDuration.TotalMilliseconds:F0}ms exceeds maximum {threshold.MaxDuration.TotalMilliseconds:F0}ms");
        }

        // Validate memory allocation
        if (memoryUsed > threshold.MaxMemoryAllocation)
        {
            violations.Add($"Memory allocation {memoryUsed:N0} bytes exceeds maximum {threshold.MaxMemoryAllocation:N0} bytes");
        }

        // Log violations
        if (violations.Any())
        {
            var violationMessage = string.Join(", ", violations);
            _logger.LogWarning("Performance threshold violations for {OperationName}: {Violations}",
                operationName, violationMessage);

            if (threshold.IsStrict)
            {
                throw new PerformanceContractViolationException(
                    operationName,
                    actualDuration,
                    threshold.MaxDuration);
            }
        }
        else
        {
            _logger.LogDebug("Performance validation passed for {OperationName}: {Duration}ms, {Memory}bytes",
                operationName, actualDuration.TotalMilliseconds, memoryUsed);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets performance statistics for all tracked operations
    /// </summary>
    public IPerformanceStatistics GetStatistics()
    {
        lock (_lockObject)
        {
            var records = _performanceRecords.ToArray();

            if (!records.Any())
            {
                return new PerformanceStatistics
                {
                    TotalOperations = 0,
                    CollectedAt = DateTime.UtcNow
                };
            }

            var successfulOps = records.Count(r => r.IsSuccess);
            var failedOps = records.Count(r => !r.IsSuccess);
            var durations = records.Where(r => r.Duration.HasValue).Select(r => r.Duration!.Value);

            return new PerformanceStatistics
            {
                TotalOperations = records.Length,
                SuccessfulOperations = successfulOps,
                FailedOperations = failedOps,
                ThresholdViolations = records.Count(r => r.ThresholdViolated),
                AverageDuration = durations.Any() ? TimeSpan.FromTicks((long)durations.Average(d => d.Ticks)) : TimeSpan.Zero,
                MaxDuration = durations.Any() ? durations.Max() : TimeSpan.Zero,
                MinDuration = durations.Any() ? durations.Min() : TimeSpan.Zero,
                TotalMemoryAllocated = records.Sum(r => r.MemoryAllocated ?? 0),
                SuccessRate = records.Length > 0 ? (double)successfulOps / records.Length * 100 : 0,
                ComplianceRate = records.Length > 0 ? (double)(records.Length - records.Count(r => r.ThresholdViolated)) / records.Length * 100 : 100,
                CollectedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Gets performance statistics for a specific operation
    /// </summary>
    public IOperationPerformanceStatistics? GetOperationStatistics(string operationName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        return _operationStatistics.TryGetValue(operationName, out var stats) ? stats : null;
    }

    /// <summary>
    /// Gets performance metrics for all operations matching a pattern
    /// </summary>
    public IEnumerable<IOperationPerformanceStatistics> GetOperationStatistics(string operationNamePattern)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationNamePattern);

        var regex = new System.Text.RegularExpressions.Regex(operationNamePattern);

        return _operationStatistics
            .Where(kvp => regex.IsMatch(kvp.Key))
            .Select(kvp => kvp.Value);
    }

    /// <summary>
    /// Resets all performance tracking data
    /// Contract: Should be used between test runs to prevent data pollution
    /// </summary>
    public void Reset()
    {
        lock (_lockObject)
        {
            _operationStatistics.Clear();
            
            // Clear the queue
            while (_performanceRecords.TryDequeue(out _))
            {
                // Just dequeue all items
            }

            _logger.LogDebug("Performance tracking data reset");
        }
    }

    private void RecordPerformance(PerformanceRecord record)
    {
        _performanceRecords.Enqueue(record);

        // Keep only the last 10,000 records to prevent memory issues
        while (_performanceRecords.Count > 10000)
        {
            _performanceRecords.TryDequeue(out _);
        }
    }

    private void UpdateOperationStatistics(PerformanceRecord record)
    {
        _operationStatistics.AddOrUpdate(
            record.OperationName,
            _ => OperationPerformanceStatistics.CreateFromRecord(record),
            (_, existing) => existing.UpdateWithRecord(record));
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            Reset();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during performance tracker disposal");
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Internal record for tracking individual performance measurements
/// </summary>
internal class PerformanceRecord
{
    public string OperationName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan? Duration { get; set; }
    public long? MemoryAllocated { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public bool ThresholdViolated { get; set; }
    public int ThreadId { get; set; }
}

/// <summary>
/// Concrete implementation of overall performance statistics
/// </summary>
internal class PerformanceStatistics : IPerformanceStatistics
{
    public int TotalOperations { get; init; }
    public int SuccessfulOperations { get; init; }
    public int FailedOperations { get; init; }
    public int ThresholdViolations { get; init; }
    public TimeSpan AverageDuration { get; init; }
    public TimeSpan MaxDuration { get; init; }
    public TimeSpan MinDuration { get; init; }
    public long TotalMemoryAllocated { get; init; }
    public double SuccessRate { get; init; }
    public double ComplianceRate { get; init; }
    public DateTime CollectedAt { get; init; }
}

/// <summary>
/// Concrete implementation of operation-specific performance statistics
/// </summary>
internal class OperationPerformanceStatistics : IOperationPerformanceStatistics
{
    private readonly List<TimeSpan> _durations = [];
    private readonly object _lock = new();

    public string OperationName { get; private set; } = string.Empty;
    public int ExecutionCount { get; private set; }
    public int SuccessfulExecutions { get; private set; }
    public TimeSpan AverageDuration { get; private set; }
    public TimeSpan FastestDuration { get; private set; } = TimeSpan.MaxValue;
    public TimeSpan SlowestDuration { get; private set; }
    public TimeSpan Percentile95Duration { get; private set; }
    public long TotalMemoryAllocated { get; private set; }
    public int ThresholdViolations { get; private set; }
    public double SuccessRate => ExecutionCount > 0 ? (double)SuccessfulExecutions / ExecutionCount * 100 : 0;

    public static OperationPerformanceStatistics CreateFromRecord(PerformanceRecord record)
    {
        var stats = new OperationPerformanceStatistics
        {
            OperationName = record.OperationName
        };
        stats.UpdateWithRecord(record);
        return stats;
    }

    public OperationPerformanceStatistics UpdateWithRecord(PerformanceRecord record)
    {
        lock (_lock)
        {
            ExecutionCount++;
            
            if (record.IsSuccess)
            {
                SuccessfulExecutions++;
            }

            if (record.ThresholdViolated)
            {
                ThresholdViolations++;
            }

            TotalMemoryAllocated += record.MemoryAllocated ?? 0;

            if (record.Duration.HasValue)
            {
                var duration = record.Duration.Value;
                _durations.Add(duration);

                if (duration < FastestDuration)
                {
                    FastestDuration = duration;
                }

                if (duration > SlowestDuration)
                {
                    SlowestDuration = duration;
                }

                // Recalculate average and percentiles
                AverageDuration = TimeSpan.FromTicks((long)_durations.Average(d => d.Ticks));

                if (_durations.Count >= 20) // Only calculate percentiles with sufficient data
                {
                    var sortedDurations = _durations.OrderBy(d => d.Ticks).ToList();
                    var index95 = Math.Min(sortedDurations.Count - 1, (int)Math.Ceiling(sortedDurations.Count * 0.95) - 1);
                    Percentile95Duration = sortedDurations[index95];
                }
            }
        }

        return this;
    }
}