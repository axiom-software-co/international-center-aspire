using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using InternationalCenter.Shared.Tests.Abstractions;
using InternationalCenter.Shared.Tests.Infrastructure;

namespace InternationalCenter.Shared.Tests.Extensions;

/// <summary>
/// Service collection extensions for registering testing infrastructure
/// Provides consistent DI container setup following Microsoft patterns
/// Medical-grade testing services with proper dependency inversion
/// </summary>
public static class TestingServiceCollectionExtensions
{
    /// <summary>
    /// Registers all core testing infrastructure services
    /// Contract: Must register all testing contracts with proper lifetimes
    /// </summary>
    public static IServiceCollection AddTestingInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register core testing services as singletons for performance
        services.TryAddSingleton<IPerformanceTracker, PerformanceTracker>();
        services.TryAddSingleton<IValidationUtilities, ValidationUtilities>();

        // Add logging if not already present
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        return services;
    }

    /// <summary>
    /// Registers testing infrastructure with custom configuration
    /// </summary>
    public static IServiceCollection AddTestingInfrastructure(
        this IServiceCollection services,
        Action<TestingOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddTestingInfrastructure();

        // Configure testing options
        services.Configure(configureOptions);

        return services;
    }

    /// <summary>
    /// Registers testing infrastructure with configuration section binding
    /// </summary>
    public static IServiceCollection AddTestingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "Testing")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddTestingInfrastructure();

        // Bind configuration
        var testingSection = configuration.GetSection(sectionName);
        if (testingSection.Exists())
        {
            services.Configure<TestingOptions>(testingSection);
        }

        return services;
    }

    /// <summary>
    /// Registers a test environment for a specific context type
    /// Contract: Must provide proper factory functions for context creation and cleanup
    /// </summary>
    public static IServiceCollection AddTestEnvironment<TContext>(
        this IServiceCollection services,
        Func<IServiceProvider, Task<TContext>> contextFactory,
        Func<TContext, Task>? contextCleanup = null)
        where TContext : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(contextFactory);

        // Ensure core infrastructure is registered
        services.AddTestingInfrastructure();

        // Register the test environment factory
        services.TryAddTransient<ITestEnvironment<TContext>>(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var logger = provider.GetRequiredService<ILogger<TestEnvironment<TContext>>>();
            var performanceTracker = provider.GetRequiredService<IPerformanceTracker>();
            var validationUtilities = provider.GetRequiredService<IValidationUtilities>();

            return new TestEnvironment<TContext>(
                provider,
                configuration,
                logger,
                performanceTracker,
                validationUtilities,
                contextFactory,
                contextCleanup);
        });

        return services;
    }

    /// <summary>
    /// Registers a test environment using a builder pattern
    /// </summary>
    public static IServiceCollection AddTestEnvironment<TContext>(
        this IServiceCollection services,
        Action<TestEnvironmentBuilder<TContext>> configure)
        where TContext : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddTestingInfrastructure();

        services.TryAddTransient<ITestEnvironment<TContext>>(provider =>
        {
            var builder = new TestEnvironmentBuilder<TContext>()
                .WithServices(provider)
                .WithConfiguration(provider.GetRequiredService<IConfiguration>())
                .WithLogger(provider.GetRequiredService<ILogger<TestEnvironment<TContext>>>())
                .WithPerformanceTracker(provider.GetRequiredService<IPerformanceTracker>())
                .WithValidationUtilities(provider.GetRequiredService<IValidationUtilities>());

            configure(builder);
            return builder.Build();
        });

        return services;
    }

    /// <summary>
    /// Registers a test data manager for a specific entity type
    /// Contract: Must provide proper factory functions for entity creation, persistence, and cleanup
    /// </summary>
    public static IServiceCollection AddTestDataManager<TEntity, TId>(
        this IServiceCollection services,
        Func<Action<TEntity>?, Task<TEntity>> entityFactory,
        Func<TEntity, Task<TEntity>>? persistAction = null,
        Func<IEnumerable<TEntity>, Task<IEnumerable<TEntity>>>? persistManyAction = null,
        Func<TId, Task<TEntity?>>? getByIdAction = null,
        Func<TEntity, Task>? validateAction = null,
        Func<TEntity[], Task>? cleanupAction = null,
        Func<TId[], Task>? cleanupByIdsAction = null)
        where TEntity : class
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(entityFactory);

        // Ensure core infrastructure is registered
        services.AddTestingInfrastructure();

        // Register the test data manager
        services.TryAddTransient<ITestDataManager<TEntity, TId>>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<TestDataManager<TEntity, TId>>>();

            return new TestDataManager<TEntity, TId>(
                logger,
                entityFactory,
                persistAction,
                persistManyAction,
                getByIdAction,
                validateAction,
                cleanupAction,
                cleanupByIdsAction);
        });

        return services;
    }

    /// <summary>
    /// Registers a test data manager using a builder pattern
    /// </summary>
    public static IServiceCollection AddTestDataManager<TEntity, TId>(
        this IServiceCollection services,
        Action<TestDataManagerBuilder<TEntity, TId>> configure)
        where TEntity : class
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddTestingInfrastructure();

        services.TryAddTransient<ITestDataManager<TEntity, TId>>(provider =>
        {
            var builder = new TestDataManagerBuilder<TEntity, TId>()
                .WithLogger(provider.GetRequiredService<ILogger<TestDataManager<TEntity, TId>>>());

            configure(builder);
            return builder.Build();
        });

        return services;
    }

    /// <summary>
    /// Registers a test data session for scoped test data management
    /// </summary>
    public static IServiceCollection AddTestDataSession(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTestingInfrastructure();

        // Register as scoped so each test gets its own session
        services.TryAddScoped<ITestDataSession>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<TestDataSession>>();
            return new TestDataSession(logger);
        });

        return services;
    }

    /// <summary>
    /// Registers Aspire testing services for distributed application testing
    /// Contract: Must provide proper integration with Aspire DistributedApplicationTestingBuilder
    /// </summary>
    public static IServiceCollection AddAspireTestingSupport(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTestingInfrastructure();

        // Add Aspire-specific testing configurations
        services.Configure<TestingOptions>(options =>
        {
            options.IntegrationTestTimeoutMs = 30000; // 30 seconds for Aspire orchestration
            options.EndToEndTestTimeoutMs = 60000;   // 60 seconds for full E2E workflows
            options.EnablePerformanceTracking = true;
            options.EnableDetailedLogging = true;
        });

        return services;
    }

    /// <summary>
    /// Registers unit testing services with optimized configuration
    /// Contract: Must provide fast, isolated testing with 5-second timeouts
    /// </summary>
    public static IServiceCollection AddUnitTestingSupport(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTestingInfrastructure();

        // Configure for unit testing performance
        services.Configure<TestingOptions>(options =>
        {
            options.UnitTestTimeoutMs = 5000;        // 5 seconds max for unit tests
            options.EnablePerformanceTracking = true;
            options.StrictPerformanceValidation = true;
            options.EnableDetailedLogging = false;   // Reduce logging for unit test performance
        });

        return services;
    }

    /// <summary>
    /// Registers integration testing services with moderate timeout configuration
    /// Contract: Must provide realistic testing with 30-second timeouts and real dependencies
    /// </summary>
    public static IServiceCollection AddIntegrationTestingSupport(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTestingInfrastructure();

        // Configure for integration testing
        services.Configure<TestingOptions>(options =>
        {
            options.IntegrationTestTimeoutMs = 30000; // 30 seconds for integration tests
            options.EnablePerformanceTracking = true;
            options.StrictPerformanceValidation = false; // More lenient for real dependencies
            options.EnableDetailedLogging = true;
        });

        return services;
    }

    /// <summary>
    /// Registers end-to-end testing services with extended timeout configuration
    /// Contract: Must provide comprehensive testing with browser automation support
    /// </summary>
    public static IServiceCollection AddEndToEndTestingSupport(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTestingInfrastructure();

        // Configure for end-to-end testing
        services.Configure<TestingOptions>(options =>
        {
            options.EndToEndTestTimeoutMs = 60000;   // 60 seconds for E2E tests
            options.EnablePerformanceTracking = true;
            options.StrictPerformanceValidation = false; // Lenient for browser automation
            options.EnableDetailedLogging = true;
            options.EnableSecurityHeaderValidation = true;
            options.EnableAccessibilityValidation = true;
        });

        return services;
    }

    /// <summary>
    /// Registers all testing services with complete configuration
    /// Contract: Must provide comprehensive testing framework setup
    /// </summary>
    public static IServiceCollection AddCompleteTestingFramework(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Register all testing infrastructure
        services.AddTestingInfrastructure(configuration);
        services.AddTestDataSession();
        services.AddAspireTestingSupport();

        // Add support for all test types
        services.AddUnitTestingSupport();
        services.AddIntegrationTestingSupport();
        services.AddEndToEndTestingSupport();

        return services;
    }
}

/// <summary>
/// Configuration options for testing infrastructure
/// Provides centralized configuration for all testing concerns
/// </summary>
public class TestingOptions
{
    /// <summary>
    /// Gets or sets the unit test timeout in milliseconds (default: 5000)
    /// </summary>
    public int UnitTestTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the integration test timeout in milliseconds (default: 30000)
    /// </summary>
    public int IntegrationTestTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets the end-to-end test timeout in milliseconds (default: 60000)
    /// </summary>
    public int EndToEndTestTimeoutMs { get; set; } = 60000;

    /// <summary>
    /// Gets or sets whether performance tracking is enabled (default: true)
    /// </summary>
    public bool EnablePerformanceTracking { get; set; } = true;

    /// <summary>
    /// Gets or sets whether strict performance validation is enforced (default: true)
    /// </summary>
    public bool StrictPerformanceValidation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether detailed logging is enabled (default: false)
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets whether security header validation is enabled (default: false)
    /// </summary>
    public bool EnableSecurityHeaderValidation { get; set; } = false;

    /// <summary>
    /// Gets or sets whether accessibility validation is enabled (default: false)
    /// </summary>
    public bool EnableAccessibilityValidation { get; set; } = false;

    /// <summary>
    /// Gets or sets whether medical-grade audit validation is enabled (default: true)
    /// </summary>
    public bool EnableMedicalGradeAuditValidation { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum memory allocation for unit tests in bytes (default: 50MB)
    /// </summary>
    public long UnitTestMaxMemoryBytes { get; set; } = 50 * 1024 * 1024;

    /// <summary>
    /// Gets or sets the maximum memory allocation for integration tests in bytes (default: 200MB)
    /// </summary>
    public long IntegrationTestMaxMemoryBytes { get; set; } = 200 * 1024 * 1024;

    /// <summary>
    /// Gets or sets the maximum memory allocation for end-to-end tests in bytes (default: 500MB)
    /// </summary>
    public long EndToEndTestMaxMemoryBytes { get; set; } = 500 * 1024 * 1024;

    /// <summary>
    /// Gets unit test performance threshold
    /// </summary>
    public PerformanceThreshold GetUnitTestThreshold() => new()
    {
        MaxDuration = TimeSpan.FromMilliseconds(UnitTestTimeoutMs),
        MaxMemoryAllocation = UnitTestMaxMemoryBytes,
        MaxCpuUsagePercent = 80,
        IsStrict = StrictPerformanceValidation,
        Category = PerformanceCategory.UnitTest
    };

    /// <summary>
    /// Gets integration test performance threshold
    /// </summary>
    public PerformanceThreshold GetIntegrationTestThreshold() => new()
    {
        MaxDuration = TimeSpan.FromMilliseconds(IntegrationTestTimeoutMs),
        MaxMemoryAllocation = IntegrationTestMaxMemoryBytes,
        MaxCpuUsagePercent = 90,
        IsStrict = StrictPerformanceValidation,
        Category = PerformanceCategory.IntegrationTest
    };

    /// <summary>
    /// Gets end-to-end test performance threshold
    /// </summary>
    public PerformanceThreshold GetEndToEndTestThreshold() => new()
    {
        MaxDuration = TimeSpan.FromMilliseconds(EndToEndTestTimeoutMs),
        MaxMemoryAllocation = EndToEndTestMaxMemoryBytes,
        MaxCpuUsagePercent = 95,
        IsStrict = false, // E2E tests are more lenient
        Category = PerformanceCategory.EndToEndTest
    };

    /// <summary>
    /// Validates the configuration options
    /// </summary>
    public void Validate()
    {
        if (UnitTestTimeoutMs <= 0 || UnitTestTimeoutMs > 60000)
        {
            throw new InvalidOperationException($"UnitTestTimeoutMs must be between 1-60000, got: {UnitTestTimeoutMs}");
        }

        if (IntegrationTestTimeoutMs <= 0 || IntegrationTestTimeoutMs > 600000)
        {
            throw new InvalidOperationException($"IntegrationTestTimeoutMs must be between 1-600000, got: {IntegrationTestTimeoutMs}");
        }

        if (EndToEndTestTimeoutMs <= 0 || EndToEndTestTimeoutMs > 600000)
        {
            throw new InvalidOperationException($"EndToEndTestTimeoutMs must be between 1-600000, got: {EndToEndTestTimeoutMs}");
        }

        if (UnitTestMaxMemoryBytes <= 0)
        {
            throw new InvalidOperationException($"UnitTestMaxMemoryBytes must be positive, got: {UnitTestMaxMemoryBytes}");
        }

        if (IntegrationTestMaxMemoryBytes <= 0)
        {
            throw new InvalidOperationException($"IntegrationTestMaxMemoryBytes must be positive, got: {IntegrationTestMaxMemoryBytes}");
        }

        if (EndToEndTestMaxMemoryBytes <= 0)
        {
            throw new InvalidOperationException($"EndToEndTestMaxMemoryBytes must be positive, got: {EndToEndTestMaxMemoryBytes}");
        }
    }
}