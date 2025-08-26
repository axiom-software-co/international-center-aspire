using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using InternationalCenter.Shared.Tests.Abstractions;

namespace InternationalCenter.Shared.Tests.Infrastructure;

/// <summary>
/// Standardized test environment implementation providing consistent setup and validation
/// Implements dependency inversion pattern for test environment management
/// Medical-grade testing environment with performance tracking and error handling
/// </summary>
public class TestEnvironment<TContext> : ITestEnvironment<TContext>
    where TContext : class
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TestEnvironment<TContext>> _logger;
    private readonly IPerformanceTracker _performanceTracker;
    private readonly IValidationUtilities _validationUtilities;
    private readonly Func<IServiceProvider, Task<TContext>> _contextFactory;
    private readonly Func<TContext, Task> _contextCleanup;
    private bool _disposed;

    public IConfiguration Configuration => _configuration;
    public IServiceProvider Services => _services;
    public ILogger Logger => _logger;

    public TestEnvironment(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger<TestEnvironment<TContext>> logger,
        IPerformanceTracker performanceTracker,
        IValidationUtilities validationUtilities,
        Func<IServiceProvider, Task<TContext>> contextFactory,
        Func<TContext, Task>? contextCleanup = null)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _performanceTracker = performanceTracker ?? throw new ArgumentNullException(nameof(performanceTracker));
        _validationUtilities = validationUtilities ?? throw new ArgumentNullException(nameof(validationUtilities));
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _contextCleanup = contextCleanup ?? (_ => Task.CompletedTask);
    }

    /// <summary>
    /// Sets up the test environment and creates the test context
    /// Contract: Must validate environment preconditions and create clean test context
    /// </summary>
    public async Task<TContext> SetupAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIfDisposed(_disposed, this);

        return await _performanceTracker.TrackAsync(
            "TestEnvironment.Setup",
            async () =>
            {
                _logger.LogInformation("Setting up test environment for context type {ContextType}", typeof(TContext).Name);

                // Validate environment before setup
                await ValidateEnvironmentAsync(cancellationToken);

                // Create the test context
                var context = await _contextFactory(_services);

                // Validate the created context
                if (context is ITestContext testContext && !testContext.IsValid())
                {
                    throw new InvalidOperationException($"Created test context is invalid: {testContext.ContextId}");
                }

                _logger.LogInformation("Test environment setup completed for context {ContextType}", typeof(TContext).Name);
                return context;
            },
            maxDuration: TimeSpan.FromSeconds(30),
            threshold: PerformanceThreshold.IntegrationTest(),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Executes an operation within the test environment with validation and performance tracking
    /// </summary>
    public async Task<T> ExecuteWithValidationAsync<T>(
        Func<TContext, Task<T>> operation,
        string operationName,
        TimeSpan? maxDuration = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIfDisposed(_disposed, this);
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        return await _performanceTracker.TrackAsync(
            $"TestEnvironment.Execute.{operationName}",
            async () =>
            {
                _logger.LogInformation("Executing operation {OperationName} in test environment", operationName);

                var context = await SetupAsync(cancellationToken);
                try
                {
                    var result = await operation(context);
                    
                    _logger.LogInformation("Operation {OperationName} completed successfully", operationName);
                    return result;
                }
                finally
                {
                    await CleanupAsync(context, cancellationToken);
                }
            },
            maxDuration: maxDuration ?? TimeSpan.FromSeconds(30),
            threshold: PerformanceThreshold.IntegrationTest(maxDuration),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Executes an operation within the test environment with validation (no return value)
    /// </summary>
    public async Task ExecuteWithValidationAsync(
        Func<TContext, Task> operation,
        string operationName,
        TimeSpan? maxDuration = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await ExecuteWithValidationAsync<object?>(
            async context =>
            {
                await operation(context);
                return null;
            },
            operationName,
            maxDuration,
            cancellationToken);
    }

    /// <summary>
    /// Cleans up the test environment and context
    /// Contract: Must ensure complete resource cleanup and data isolation
    /// </summary>
    public async Task CleanupAsync(TContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        await _performanceTracker.TrackAsync(
            "TestEnvironment.Cleanup",
            async () =>
            {
                _logger.LogInformation("Cleaning up test environment for context type {ContextType}", typeof(TContext).Name);

                try
                {
                    await _contextCleanup(context);

                    if (context is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync();
                    }
                    else if (context is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }

                    _logger.LogInformation("Test environment cleanup completed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during test environment cleanup");
                    throw;
                }
            },
            maxDuration: TimeSpan.FromSeconds(10),
            threshold: PerformanceThreshold.IntegrationTest(TimeSpan.FromSeconds(10)),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Validates that the test environment is properly configured
    /// Contract: Must throw descriptive exceptions for configuration issues
    /// </summary>
    public async Task ValidateEnvironmentAsync(CancellationToken cancellationToken = default)
    {
        await _performanceTracker.TrackAsync(
            "TestEnvironment.Validation",
            async () =>
            {
                _logger.LogDebug("Validating test environment configuration");

                // Validate required services are registered
                if (_services.GetService<IPerformanceTracker>() == null)
                {
                    throw new InvalidOperationException("IPerformanceTracker is not registered in the service container");
                }

                if (_services.GetService<IValidationUtilities>() == null)
                {
                    throw new InvalidOperationException("IValidationUtilities is not registered in the service container");
                }

                // Validate configuration sections
                var testConfig = _configuration.GetSection("Testing");
                if (!testConfig.Exists())
                {
                    _logger.LogWarning("Testing configuration section not found, using defaults");
                }

                // Validate performance thresholds are reasonable
                var unitTestTimeout = _configuration.GetValue<int?>("Testing:UnitTestTimeoutMs") ?? 5000;
                var integrationTestTimeout = _configuration.GetValue<int?>("Testing:IntegrationTestTimeoutMs") ?? 30000;

                if (unitTestTimeout <= 0 || unitTestTimeout > 60000)
                {
                    throw new InvalidOperationException($"Invalid unit test timeout: {unitTestTimeout}ms. Must be between 1-60000ms");
                }

                if (integrationTestTimeout <= 0 || integrationTestTimeout > 600000)
                {
                    throw new InvalidOperationException($"Invalid integration test timeout: {integrationTestTimeout}ms. Must be between 1-600000ms");
                }

                _logger.LogDebug("Test environment validation completed successfully");
                await Task.CompletedTask;
            },
            maxDuration: TimeSpan.FromSeconds(5),
            threshold: PerformanceThreshold.UnitTest(),
            cancellationToken: cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        try
        {
            _performanceTracker?.Dispose();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error disposing performance tracker during test environment cleanup");
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Builder for creating TestEnvironment instances with fluent configuration
/// </summary>
public class TestEnvironmentBuilder<TContext>
    where TContext : class
{
    private IServiceProvider? _services;
    private IConfiguration? _configuration;
    private ILogger<TestEnvironment<TContext>>? _logger;
    private IPerformanceTracker? _performanceTracker;
    private IValidationUtilities? _validationUtilities;
    private Func<IServiceProvider, Task<TContext>>? _contextFactory;
    private Func<TContext, Task>? _contextCleanup;

    public TestEnvironmentBuilder<TContext> WithServices(IServiceProvider services)
    {
        _services = services;
        return this;
    }

    public TestEnvironmentBuilder<TContext> WithConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
        return this;
    }

    public TestEnvironmentBuilder<TContext> WithLogger(ILogger<TestEnvironment<TContext>> logger)
    {
        _logger = logger;
        return this;
    }

    public TestEnvironmentBuilder<TContext> WithPerformanceTracker(IPerformanceTracker performanceTracker)
    {
        _performanceTracker = performanceTracker;
        return this;
    }

    public TestEnvironmentBuilder<TContext> WithValidationUtilities(IValidationUtilities validationUtilities)
    {
        _validationUtilities = validationUtilities;
        return this;
    }

    public TestEnvironmentBuilder<TContext> WithContextFactory(Func<IServiceProvider, Task<TContext>> contextFactory)
    {
        _contextFactory = contextFactory;
        return this;
    }

    public TestEnvironmentBuilder<TContext> WithContextCleanup(Func<TContext, Task> contextCleanup)
    {
        _contextCleanup = contextCleanup;
        return this;
    }

    public TestEnvironment<TContext> Build()
    {
        if (_services == null) throw new InvalidOperationException("Services must be provided");
        if (_configuration == null) throw new InvalidOperationException("Configuration must be provided");
        if (_logger == null) throw new InvalidOperationException("Logger must be provided");
        if (_performanceTracker == null) throw new InvalidOperationException("PerformanceTracker must be provided");
        if (_validationUtilities == null) throw new InvalidOperationException("ValidationUtilities must be provided");
        if (_contextFactory == null) throw new InvalidOperationException("ContextFactory must be provided");

        return new TestEnvironment<TContext>(
            _services,
            _configuration,
            _logger,
            _performanceTracker,
            _validationUtilities,
            _contextFactory,
            _contextCleanup);
    }
}