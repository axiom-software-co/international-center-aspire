using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InternationalCenter.Shared.Tests.Abstractions;

/// <summary>
/// Contract for test environment setup and management with dependency inversion
/// Provides consistent test environment configuration across all test types and domains
/// Medical-grade testing environment with standardized setup, validation, and cleanup
/// </summary>
/// <typeparam name="TContext">The test context type specific to the testing domain</typeparam>
public interface ITestEnvironment<TContext> : IAsyncDisposable
    where TContext : class
{
    /// <summary>
    /// Gets the test environment configuration
    /// </summary>
    IConfiguration Configuration { get; }
    
    /// <summary>
    /// Gets the test environment service provider
    /// </summary>
    IServiceProvider Services { get; }
    
    /// <summary>
    /// Gets the test environment logger
    /// </summary>
    ILogger Logger { get; }
    
    /// <summary>
    /// Sets up the test environment and returns the context
    /// Contract: Must validate environment preconditions and create clean test context
    /// </summary>
    Task<TContext> SetupAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes an operation within the test environment with validation
    /// Contract: Must provide performance tracking and error handling
    /// </summary>
    Task<T> ExecuteWithValidationAsync<T>(
        Func<TContext, Task<T>> operation,
        string operationName,
        TimeSpan? maxDuration = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes an operation within the test environment with validation (no return value)
    /// </summary>
    Task ExecuteWithValidationAsync(
        Func<TContext, Task> operation,
        string operationName,
        TimeSpan? maxDuration = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cleans up the test environment and context
    /// Contract: Must ensure complete resource cleanup and data isolation
    /// </summary>
    Task CleanupAsync(TContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates that the test environment is properly configured
    /// Contract: Must throw descriptive exceptions for configuration issues
    /// </summary>
    Task ValidateEnvironmentAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Base test context interface for all test contexts
/// Provides common context properties and behavior contracts
/// </summary>
public interface ITestContext
{
    /// <summary>
    /// Gets the unique identifier for this test context instance
    /// </summary>
    string ContextId { get; }
    
    /// <summary>
    /// Gets the test execution timestamp
    /// </summary>
    DateTime CreatedAt { get; }
    
    /// <summary>
    /// Gets or sets additional context metadata
    /// </summary>
    IDictionary<string, object> Metadata { get; }
    
    /// <summary>
    /// Validates that the context is in a valid state
    /// </summary>
    bool IsValid();
}