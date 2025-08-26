using System.Net;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace InternationalCenter.Tests.Shared.Utils;

/// <summary>
/// Provides exponential backoff retry strategies for integration tests
/// WHY: CI/CD environments with distributed infrastructure can have transient failures
/// SCOPE: All integration tests needing resilient HTTP, database, and cache operations
/// CONTEXT: Gateway orchestration with Aspire requires environment-aware timeout strategies
/// </summary>
public static class RetryHelper
{
    /// <summary>
    /// Default retry configuration for HTTP operations in gateway tests
    /// </summary>
    public static readonly RetryConfig DefaultHttpRetry = new()
    {
        MaxAttempts = 3,
        BaseDelayMs = 1000,
        MaxDelayMs = 10000,
        BackoffMultiplier = 2.0,
        RetryableStatusCodes = new[]
        {
            HttpStatusCode.InternalServerError,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.GatewayTimeout,
            HttpStatusCode.TooManyRequests
        }
    };

    /// <summary>
    /// Stricter retry configuration for database operations
    /// </summary>
    public static readonly RetryConfig DefaultDatabaseRetry = new()
    {
        MaxAttempts = 5,
        BaseDelayMs = 500,
        MaxDelayMs = 5000,
        BackoffMultiplier = 1.5,
        RetryableStatusCodes = Array.Empty<HttpStatusCode>()
    };

    /// <summary>
    /// Fast retry configuration for cache operations
    /// </summary>
    public static readonly RetryConfig DefaultCacheRetry = new()
    {
        MaxAttempts = 2,
        BaseDelayMs = 100,
        MaxDelayMs = 1000,
        BackoffMultiplier = 2.0,
        RetryableStatusCodes = Array.Empty<HttpStatusCode>()
    };

    /// <summary>
    /// Execute HTTP operations with exponential backoff retry strategy
    /// </summary>
    public static async Task<HttpResponseMessage> ExecuteHttpWithRetryAsync(
        Func<Task<HttpResponseMessage>> operation,
        RetryConfig? config = null,
        ITestOutputHelper? output = null,
        string? operationName = null)
    {
        config ??= DefaultHttpRetry;
        var attempt = 1;
        var delay = config.BaseDelayMs;

        while (attempt <= config.MaxAttempts)
        {
            try
            {
                var response = await operation();
                
                // Check if response indicates a retryable failure
                if (config.RetryableStatusCodes.Contains(response.StatusCode))
                {
                    if (attempt == config.MaxAttempts)
                    {
                        output?.WriteLine($"⚠️  HTTP RETRY: Final attempt {attempt}/{config.MaxAttempts} for {operationName ?? "HTTP operation"} failed with {response.StatusCode}");
                        return response; // Return the failed response on final attempt
                    }
                    
                    output?.WriteLine($"🔄 HTTP RETRY: Attempt {attempt}/{config.MaxAttempts} for {operationName ?? "HTTP operation"} failed with {response.StatusCode}, retrying after {delay}ms");
                    await Task.Delay(delay);
                    delay = Math.Min((int)(delay * config.BackoffMultiplier), config.MaxDelayMs);
                    attempt++;
                    continue;
                }

                // Success or non-retryable failure
                if (attempt > 1)
                {
                    output?.WriteLine($"✅ HTTP RETRY: {operationName ?? "HTTP operation"} succeeded on attempt {attempt}/{config.MaxAttempts}");
                }
                return response;
            }
            catch (HttpRequestException ex) when (attempt < config.MaxAttempts)
            {
                output?.WriteLine($"🔄 HTTP RETRY: Attempt {attempt}/{config.MaxAttempts} for {operationName ?? "HTTP operation"} failed with {ex.Message}, retrying after {delay}ms");
                await Task.Delay(delay);
                delay = Math.Min((int)(delay * config.BackoffMultiplier), config.MaxDelayMs);
                attempt++;
            }
            catch (TaskCanceledException ex) when (attempt < config.MaxAttempts)
            {
                output?.WriteLine($"🔄 HTTP RETRY: Attempt {attempt}/{config.MaxAttempts} for {operationName ?? "HTTP operation"} timed out: {ex.Message}, retrying after {delay}ms");
                await Task.Delay(delay);
                delay = Math.Min((int)(delay * config.BackoffMultiplier), config.MaxDelayMs);
                attempt++;
            }
        }

        // This should never be reached due to the logic above, but included for completeness
        throw new InvalidOperationException("Retry logic error");
    }

    /// <summary>
    /// Execute async operations with exponential backoff retry strategy
    /// </summary>
    public static async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        RetryConfig? config = null,
        ITestOutputHelper? output = null,
        string? operationName = null,
        Predicate<Exception>? isRetryableException = null)
    {
        config ??= DefaultDatabaseRetry;
        var attempt = 1;
        var delay = config.BaseDelayMs;

        isRetryableException ??= DefaultIsRetryableException;

        while (attempt <= config.MaxAttempts)
        {
            try
            {
                var result = await operation();
                if (attempt > 1)
                {
                    output?.WriteLine($"✅ ASYNC RETRY: {operationName ?? "Operation"} succeeded on attempt {attempt}/{config.MaxAttempts}");
                }
                return result;
            }
            catch (Exception ex) when (attempt < config.MaxAttempts && isRetryableException(ex))
            {
                output?.WriteLine($"🔄 ASYNC RETRY: Attempt {attempt}/{config.MaxAttempts} for {operationName ?? "Operation"} failed with {ex.GetType().Name}: {ex.Message}, retrying after {delay}ms");
                await Task.Delay(delay);
                delay = Math.Min((int)(delay * config.BackoffMultiplier), config.MaxDelayMs);
                attempt++;
            }
        }

        // This should never be reached due to the logic above, but included for completeness
        throw new InvalidOperationException("Retry logic error");
    }

    /// <summary>
    /// Execute async operations without return value with exponential backoff retry strategy
    /// </summary>
    public static async Task ExecuteWithRetryAsync(
        Func<Task> operation,
        RetryConfig? config = null,
        ITestOutputHelper? output = null,
        string? operationName = null,
        Predicate<Exception>? isRetryableException = null)
    {
        await ExecuteWithRetryAsync(async () =>
        {
            await operation();
            return true; // Dummy return value
        }, config, output, operationName, isRetryableException);
    }

    /// <summary>
    /// Default predicate to determine if an exception is retryable
    /// </summary>
    private static bool DefaultIsRetryableException(Exception ex)
    {
        return ex is HttpRequestException
            || ex is TaskCanceledException
            || ex is TimeoutException
            || ex is InvalidOperationException when ex.Message.Contains("connection")
            || ex is System.Data.Common.DbException
            || ex is Npgsql.NpgsqlException
            || (ex.InnerException != null && DefaultIsRetryableException(ex.InnerException));
    }
}

/// <summary>
/// Configuration for retry strategies
/// </summary>
public record RetryConfig
{
    /// <summary>
    /// Maximum number of attempts (including initial attempt)
    /// </summary>
    public int MaxAttempts { get; init; } = 3;

    /// <summary>
    /// Base delay in milliseconds before first retry
    /// </summary>
    public int BaseDelayMs { get; init; } = 1000;

    /// <summary>
    /// Maximum delay in milliseconds between retries
    /// </summary>
    public int MaxDelayMs { get; init; } = 10000;

    /// <summary>
    /// Multiplier for exponential backoff
    /// </summary>
    public double BackoffMultiplier { get; init; } = 2.0;

    /// <summary>
    /// HTTP status codes that should trigger a retry
    /// </summary>
    public HttpStatusCode[] RetryableStatusCodes { get; init; } = Array.Empty<HttpStatusCode>();
}