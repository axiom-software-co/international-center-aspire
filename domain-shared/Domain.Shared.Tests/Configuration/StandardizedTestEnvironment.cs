using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Xunit.Abstractions;

namespace InternationalCenter.Tests.Shared.Configuration;

/// <summary>
/// Standardized test environment configuration for all Services API test projects
/// Eliminates configuration duplication and provides consistent test setup patterns
/// Medical-grade test environment configuration ensuring reliable and reproducible testing
/// </summary>
public static class StandardizedTestEnvironment
{
    private static readonly object _lock = new object();
    private static IConfiguration? _configuration;
    private static IHost? _testHost;
    
    /// <summary>
    /// Gets the standardized test configuration
    /// Provides consistent configuration loading across all test projects
    /// </summary>
    public static IConfiguration Configuration
    {
        get
        {
            if (_configuration == null)
            {
                lock (_lock)
                {
                    if (_configuration == null)
                    {
                        _configuration = BuildStandardizedConfiguration();
                    }
                }
            }
            return _configuration;
        }
    }
    
    /// <summary>
    /// Gets the standardized test host
    /// Provides consistent DI container setup for all test projects
    /// </summary>
    public static IHost TestHost
    {
        get
        {
            if (_testHost == null)
            {
                lock (_lock)
                {
                    if (_testHost == null)
                    {
                        _testHost = BuildStandardizedTestHost();
                    }
                }
            }
            return _testHost;
        }
    }
    
    /// <summary>
    /// Creates a standardized service collection for test projects
    /// Eliminates DI setup duplication across test classes
    /// </summary>
    public static IServiceCollection CreateStandardizedServiceCollection()
    {
        var services = new ServiceCollection();
        
        // Add standardized configuration
        services.AddSingleton(Configuration);
        
        // Add standardized logging
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddConfiguration(Configuration.GetSection("Logging"));
            builder.SetMinimumLevel(GetTestLogLevel());
            
            // Add test-specific logging providers
            builder.Services.AddSingleton<ILoggerProvider, TestLoggerProvider>();
        });
        
        // Add standardized test utilities
        services.AddSingleton<TestEnvironmentInfo>();
        services.AddSingleton<TestDataManager>();
        services.AddSingleton<TestPerformanceTracker>();
        
        return services;
    }
    
    /// <summary>
    /// Creates a scoped test environment for individual test classes
    /// Provides isolated test environment while maintaining standardized configuration
    /// </summary>
    public static TestEnvironmentScope CreateTestScope(ITestOutputHelper output, string testClassName)
    {
        return new TestEnvironmentScope(TestHost.Services.CreateScope(), output, testClassName);
    }
    
    /// <summary>
    /// Gets standardized test timeouts for different test types
    /// Provides consistent timeout configuration across all test projects
    /// </summary>
    public static StandardizedTestTimeouts GetTestTimeouts()
    {
        return new StandardizedTestTimeouts
        {
            UnitTestTimeout = Configuration.GetValue<int>("Testing:Timeouts:UnitTest", 5000),
            IntegrationTestTimeout = Configuration.GetValue<int>("Testing:Timeouts:IntegrationTest", 30000),
            EndToEndTestTimeout = Configuration.GetValue<int>("Testing:Timeouts:EndToEndTest", 60000),
            LoadTestTimeout = Configuration.GetValue<int>("Testing:Timeouts:LoadTest", 300000),
            DatabaseOperationTimeout = Configuration.GetValue<int>("Testing:Timeouts:DatabaseOperation", 10000),
            HttpRequestTimeout = Configuration.GetValue<int>("Testing:Timeouts:HttpRequest", 30000)
        };
    }
    
    /// <summary>
    /// Gets standardized test data configuration
    /// Provides consistent test data management across projects
    /// </summary>
    public static TestDataConfiguration GetTestDataConfiguration()
    {
        return new TestDataConfiguration
        {
            DefaultPageSize = Configuration.GetValue<int>("Testing:Data:DefaultPageSize", 10),
            MaxTestEntities = Configuration.GetValue<int>("Testing:Data:MaxTestEntities", 100),
            CleanupStrategy = Configuration.GetValue<string>("Testing:Data:CleanupStrategy", "Respawn"),
            SeedDataEnabled = Configuration.GetValue<bool>("Testing:Data:SeedDataEnabled", true),
            GenerateRealisticData = Configuration.GetValue<bool>("Testing:Data:GenerateRealisticData", true)
        };
    }
    
    /// <summary>
    /// Gets standardized performance test configuration
    /// Provides consistent performance validation across test projects
    /// </summary>
    public static PerformanceTestConfiguration GetPerformanceConfiguration()
    {
        return new PerformanceTestConfiguration
        {
            EnablePerformanceValidation = Configuration.GetValue<bool>("Testing:Performance:EnableValidation", true),
            UnitTestMaxDuration = TimeSpan.FromMilliseconds(Configuration.GetValue<int>("Testing:Performance:UnitTestMaxMs", 100)),
            IntegrationTestMaxDuration = TimeSpan.FromMilliseconds(Configuration.GetValue<int>("Testing:Performance:IntegrationTestMaxMs", 2000)),
            DatabaseOperationMaxDuration = TimeSpan.FromMilliseconds(Configuration.GetValue<int>("Testing:Performance:DatabaseOperationMaxMs", 1000)),
            CollectDetailedMetrics = Configuration.GetValue<bool>("Testing:Performance:CollectDetailedMetrics", false)
        };
    }
    
    /// <summary>
    /// Gets standardized Aspire test configuration
    /// Provides consistent Aspire orchestration setup across integration tests
    /// </summary>
    public static AspireTestConfiguration GetAspireConfiguration()
    {
        return new AspireTestConfiguration
        {
            StartTimeout = TimeSpan.FromSeconds(Configuration.GetValue<int>("Testing:Aspire:StartTimeoutSeconds", 60)),
            ShutdownTimeout = TimeSpan.FromSeconds(Configuration.GetValue<int>("Testing:Aspire:ShutdownTimeoutSeconds", 30)),
            EnableResourceLogging = Configuration.GetValue<bool>("Testing:Aspire:EnableResourceLogging", true),
            WaitForHealthChecks = Configuration.GetValue<bool>("Testing:Aspire:WaitForHealthChecks", true),
            HealthCheckTimeout = TimeSpan.FromSeconds(Configuration.GetValue<int>("Testing:Aspire:HealthCheckTimeoutSeconds", 30)),
            RetryAttempts = Configuration.GetValue<int>("Testing:Aspire:RetryAttempts", 3),
            RetryDelay = TimeSpan.FromSeconds(Configuration.GetValue<int>("Testing:Aspire:RetryDelaySeconds", 2))
        };
    }
    
    /// <summary>
    /// Validates that the test environment is properly configured
    /// Ensures all required configuration sections are present and valid
    /// </summary>
    public static void ValidateTestEnvironment()
    {
        var requiredSections = new[]
        {
            "Testing:Timeouts",
            "Testing:Data", 
            "Testing:Performance",
            "Testing:Aspire",
            "ConnectionStrings",
            "Logging"
        };
        
        var missingSections = new List<string>();
        
        foreach (var section in requiredSections)
        {
            if (!Configuration.GetSection(section).Exists())
            {
                missingSections.Add(section);
            }
        }
        
        if (missingSections.Any())
        {
            throw new InvalidOperationException(
                $"Test environment validation failed. Missing configuration sections: {string.Join(", ", missingSections)}");
        }
        
        // Validate critical test settings
        var timeouts = GetTestTimeouts();
        if (timeouts.UnitTestTimeout <= 0 || timeouts.IntegrationTestTimeout <= 0)
        {
            throw new InvalidOperationException("Invalid timeout configuration detected");
        }
        
        // Validate Aspire configuration
        var aspireConfig = GetAspireConfiguration();
        if (aspireConfig.StartTimeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Invalid Aspire configuration detected");
        }
    }
    
    private static IConfiguration BuildStandardizedConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(GetTestProjectRootPath())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Testing.json", optional: true)
            .AddJsonFile($"appsettings.{GetTestEnvironmentName()}.json", optional: true)
            .AddEnvironmentVariables("TESTING_")
            .AddInMemoryCollection(GetDefaultTestSettings());
        
        return builder.Build();
    }
    
    private static IHost BuildStandardizedTestHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.Sources.Clear();
                config.AddConfiguration(Configuration);
            })
            .ConfigureServices((context, services) =>
            {
                var standardizedServices = CreateStandardizedServiceCollection();
                foreach (var service in standardizedServices)
                {
                    services.Add(service);
                }
            })
            .Build();
    }
    
    private static LogLevel GetTestLogLevel()
    {
        var logLevelString = Configuration.GetValue<string>("Testing:LogLevel", "Information");
        return Enum.TryParse<LogLevel>(logLevelString, out var logLevel) ? logLevel : LogLevel.Information;
    }
    
    private static string GetTestProjectRootPath()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var directory = new DirectoryInfo(Path.GetDirectoryName(assemblyLocation)!);
        
        // Walk up the directory tree to find the project root (containing .csproj)
        while (directory != null && !directory.GetFiles("*.csproj").Any())
        {
            directory = directory.Parent;
        }
        
        return directory?.FullName ?? throw new InvalidOperationException("Could not locate test project root");
    }
    
    private static string GetTestEnvironmentName()
    {
        return Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? 
               Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
               "Testing";
    }
    
    private static Dictionary<string, string> GetDefaultTestSettings()
    {
        return new Dictionary<string, string>
        {
            // Default timeout settings (milliseconds)
            { "Testing:Timeouts:UnitTest", "5000" },
            { "Testing:Timeouts:IntegrationTest", "30000" },
            { "Testing:Timeouts:EndToEndTest", "60000" },
            { "Testing:Timeouts:LoadTest", "300000" },
            { "Testing:Timeouts:DatabaseOperation", "10000" },
            { "Testing:Timeouts:HttpRequest", "30000" },
            
            // Default data settings
            { "Testing:Data:DefaultPageSize", "10" },
            { "Testing:Data:MaxTestEntities", "100" },
            { "Testing:Data:CleanupStrategy", "Respawn" },
            { "Testing:Data:SeedDataEnabled", "true" },
            { "Testing:Data:GenerateRealisticData", "true" },
            
            // Default performance settings
            { "Testing:Performance:EnableValidation", "true" },
            { "Testing:Performance:UnitTestMaxMs", "100" },
            { "Testing:Performance:IntegrationTestMaxMs", "2000" },
            { "Testing:Performance:DatabaseOperationMaxMs", "1000" },
            { "Testing:Performance:CollectDetailedMetrics", "false" },
            
            // Default Aspire settings
            { "Testing:Aspire:StartTimeoutSeconds", "60" },
            { "Testing:Aspire:ShutdownTimeoutSeconds", "30" },
            { "Testing:Aspire:EnableResourceLogging", "true" },
            { "Testing:Aspire:WaitForHealthChecks", "true" },
            { "Testing:Aspire:HealthCheckTimeoutSeconds", "30" },
            { "Testing:Aspire:RetryAttempts", "3" },
            { "Testing:Aspire:RetryDelaySeconds", "2" },
            
            // Default logging settings
            { "Testing:LogLevel", "Information" },
            { "Logging:LogLevel:Default", "Information" },
            { "Logging:LogLevel:Microsoft.Hosting.Lifetime", "Information" },
            { "Logging:LogLevel:Microsoft.AspNetCore", "Warning" }
        };
    }
}

/// <summary>
/// Standardized timeout configuration for test types
/// </summary>
public class StandardizedTestTimeouts
{
    public int UnitTestTimeout { get; init; }
    public int IntegrationTestTimeout { get; init; }
    public int EndToEndTestTimeout { get; init; }
    public int LoadTestTimeout { get; init; }
    public int DatabaseOperationTimeout { get; init; }
    public int HttpRequestTimeout { get; init; }
}

/// <summary>
/// Standardized test data configuration
/// </summary>
public class TestDataConfiguration
{
    public int DefaultPageSize { get; init; }
    public int MaxTestEntities { get; init; }
    public string CleanupStrategy { get; init; } = "Respawn";
    public bool SeedDataEnabled { get; init; }
    public bool GenerateRealisticData { get; init; }
}

/// <summary>
/// Standardized performance test configuration
/// </summary>
public class PerformanceTestConfiguration
{
    public bool EnablePerformanceValidation { get; init; }
    public TimeSpan UnitTestMaxDuration { get; init; }
    public TimeSpan IntegrationTestMaxDuration { get; init; }
    public TimeSpan DatabaseOperationMaxDuration { get; init; }
    public bool CollectDetailedMetrics { get; init; }
}

/// <summary>
/// Standardized Aspire test configuration
/// </summary>
public class AspireTestConfiguration
{
    public TimeSpan StartTimeout { get; init; }
    public TimeSpan ShutdownTimeout { get; init; }
    public bool EnableResourceLogging { get; init; }
    public bool WaitForHealthChecks { get; init; }
    public TimeSpan HealthCheckTimeout { get; init; }
    public int RetryAttempts { get; init; }
    public TimeSpan RetryDelay { get; init; }
}

/// <summary>
/// Test environment scope for individual test classes
/// Provides isolated test environment with standardized configuration
/// </summary>
public class TestEnvironmentScope : IDisposable
{
    public IServiceScope ServiceScope { get; }
    public ITestOutputHelper Output { get; }
    public string TestClassName { get; }
    public IServiceProvider Services => ServiceScope.ServiceProvider;
    
    private bool _disposed = false;
    
    internal TestEnvironmentScope(IServiceScope serviceScope, ITestOutputHelper output, string testClassName)
    {
        ServiceScope = serviceScope;
        Output = output;
        TestClassName = testClassName;
        
        // Log test environment initialization
        var logger = Services.GetRequiredService<ILogger<TestEnvironmentScope>>();
        logger.LogInformation("Initialized test environment scope for {TestClassName}", testClassName);
    }
    
    public T GetRequiredService<T>() where T : notnull
    {
        return Services.GetRequiredService<T>();
    }
    
    public T? GetService<T>()
    {
        return Services.GetService<T>();
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            ServiceScope?.Dispose();
            _disposed = true;
        }
    }
}