using InternationalCenter.Tests.Shared.Utils;

namespace InternationalCenter.Tests.Shared.Configuration;

/// <summary>
/// Standardized test configurations for consistent testing across all Services API test projects
/// WHY: Consistent configuration prevents environment-specific failures and improves test reliability
/// SCOPE: All Services API test projects (Unit, Integration, E2E)
/// CONTEXT: Aspire distributed application testing requires standardized timeout and retry strategies
/// </summary>
public static class StandardizedTestConfiguration
{
    /// <summary>
    /// Standardized timeout configurations for different test operation types
    /// Ensures consistent timing expectations across all test environments
    /// </summary>
    public static class Timeouts
    {
        /// <summary>HTTP operation timeouts for API requests</summary>
        public static readonly TimeSpan HttpRequest = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan HttpRequestLong = TimeSpan.FromSeconds(60);
        public static readonly TimeSpan HttpRequestShort = TimeSpan.FromSeconds(10);

        /// <summary>Database operation timeouts</summary>
        public static readonly TimeSpan DatabaseOperation = TimeSpan.FromSeconds(45);
        public static readonly TimeSpan DatabaseQuery = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan DatabaseCleanup = TimeSpan.FromSeconds(60);

        /// <summary>Cache operation timeouts</summary>
        public static readonly TimeSpan CacheOperation = TimeSpan.FromSeconds(10);
        public static readonly TimeSpan CacheFlush = TimeSpan.FromSeconds(30);

        /// <summary>E2E operation timeouts</summary>
        public static readonly TimeSpan E2ENavigation = TimeSpan.FromSeconds(45);
        public static readonly TimeSpan E2EWait = TimeSpan.FromSeconds(60);
        public static readonly TimeSpan E2EWorkflow = TimeSpan.FromMinutes(3);

        /// <summary>Aspire infrastructure timeouts</summary>
        public static readonly TimeSpan AspireStartup = TimeSpan.FromSeconds(90);
        public static readonly TimeSpan AspireHealthCheck = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan AspireServiceDiscovery = TimeSpan.FromSeconds(15);

        /// <summary>Integration test timeouts</summary>
        public static readonly TimeSpan IntegrationTest = TimeSpan.FromMinutes(2);
        public static readonly TimeSpan IntegrationWorkflow = TimeSpan.FromMinutes(5);

        /// <summary>Unit test timeouts</summary>
        public static readonly TimeSpan UnitTest = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan UnitTestQuick = TimeSpan.FromSeconds(10);
    }

    /// <summary>
    /// Standardized retry configurations for different operation types
    /// Provides consistent retry behavior across distributed test environments
    /// </summary>
    public static class RetryConfigurations
    {
        /// <summary>Standard HTTP request retry configuration</summary>
        public static readonly RetryConfig HttpStandard = new()
        {
            MaxAttempts = 5,
            BaseDelayMs = 1000,
            MaxDelayMs = 10000,
            BackoffMultiplier = 1.8
        };

        /// <summary>Aggressive HTTP retry for critical operations</summary>
        public static readonly RetryConfig HttpAggressive = new()
        {
            MaxAttempts = 8,
            BaseDelayMs = 500,
            MaxDelayMs = 15000,
            BackoffMultiplier = 2.0
        };

        /// <summary>Conservative HTTP retry for load testing</summary>
        public static readonly RetryConfig HttpConservative = new()
        {
            MaxAttempts = 3,
            BaseDelayMs = 2000,
            MaxDelayMs = 8000,
            BackoffMultiplier = 1.5
        };

        /// <summary>Database operation retry configuration</summary>
        public static readonly RetryConfig Database = new()
        {
            MaxAttempts = 5,
            BaseDelayMs = 2000,
            MaxDelayMs = 15000,
            BackoffMultiplier = 1.5
        };

        /// <summary>Cache operation retry configuration</summary>
        public static readonly RetryConfig Cache = new()
        {
            MaxAttempts = 3,
            BaseDelayMs = 500,
            MaxDelayMs = 5000,
            BackoffMultiplier = 2.0
        };

        /// <summary>E2E navigation retry configuration</summary>
        public static readonly RetryConfig E2ENavigation = new()
        {
            MaxAttempts = 5,
            BaseDelayMs = 3000,
            MaxDelayMs = 15000,
            BackoffMultiplier = 2.0
        };

        /// <summary>E2E wait operation retry configuration</summary>
        public static readonly RetryConfig E2EWait = new()
        {
            MaxAttempts = 10,
            BaseDelayMs = 2000,
            MaxDelayMs = 10000,
            BackoffMultiplier = 1.2
        };

        /// <summary>Aspire infrastructure retry configuration</summary>
        public static readonly RetryConfig AspireInfrastructure = new()
        {
            MaxAttempts = 5,
            BaseDelayMs = 2000,
            MaxDelayMs = 15000,
            BackoffMultiplier = 1.5
        };

        /// <summary>Health check retry configuration</summary>
        public static readonly RetryConfig HealthCheck = new()
        {
            MaxAttempts = 5,
            BaseDelayMs = 1000,
            MaxDelayMs = 8000,
            BackoffMultiplier = 1.5
        };
    }

    /// <summary>
    /// Standardized Playwright configurations for consistent E2E testing
    /// Ensures reliable browser automation across all environments
    /// </summary>
    public static class PlaywrightConfiguration
    {
        /// <summary>Standard browser launch options</summary>
        public static readonly string[] StandardBrowserArgs = new[]
        {
            "--disable-web-security",
            "--disable-features=VizDisplayCompositor",
            "--no-sandbox",
            "--disable-dev-shm-usage", // Stability enhancement
            "--disable-gpu",
            "--disable-background-timer-throttling", // Consistent timing
            "--disable-backgrounding-occluded-windows"
        };

        /// <summary>Standard viewport size for consistent testing</summary>
        public static readonly (int Width, int Height) StandardViewport = (1920, 1080);

        /// <summary>Standard user agent for test identification</summary>
        public const string StandardUserAgent = "Standardized-Test-Agent/1.0 (Aspire Testing Infrastructure)";

        /// <summary>Standard locale and timezone for consistent results</summary>
        public const string StandardLocale = "en-US";
        public const string StandardTimezone = "UTC";

        /// <summary>Standard slow motion delay for more reliable testing</summary>
        public const int StandardSlowMotionMs = 50;

        /// <summary>Standard page navigation timeout</summary>
        public const int NavigationTimeoutMs = 45000;

        /// <summary>Standard test step delay</summary>
        public const int TestStepDelayMs = 1000;
    }

    /// <summary>
    /// Standardized test data configurations for consistent test isolation
    /// Provides consistent test data management across all test types
    /// </summary>
    public static class TestDataConfiguration
    {
        /// <summary>Standard test category configuration</summary>
        public static readonly (string Name, string Slug, string Description) StandardTestCategory = 
            ("Standardized Test Category", "standardized-test-category", "Category for standardized testing infrastructure");

        /// <summary>Standard test service configuration</summary>
        public static readonly (string Title, string Description) StandardTestService = 
            ("Standardized Test Service", "Service created through standardized testing infrastructure");

        /// <summary>Standard test identifiers</summary>
        public const string TestContextHeader = "X-Test-Context";
        public const string TestTimestampHeader = "X-Test-Timestamp";
        public const string TestInfrastructureHeader = "X-Test-Infrastructure";
        public const string TestCorrelationHeader = "X-Correlation-ID";

        /// <summary>Standard test infrastructure identifier</summary>
        public const string InfrastructureIdentifier = "Standardized-Aspire-Testing";
    }

    /// <summary>
    /// Standardized performance thresholds for consistent validation
    /// Defines acceptable performance criteria across all test environments
    /// </summary>
    public static class PerformanceThresholds
    {
        /// <summary>API response time thresholds</summary>
        public static readonly TimeSpan ApiResponseFast = TimeSpan.FromMilliseconds(500);
        public static readonly TimeSpan ApiResponseNormal = TimeSpan.FromSeconds(2);
        public static readonly TimeSpan ApiResponseSlow = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan ApiResponseMax = TimeSpan.FromSeconds(10);

        /// <summary>Database operation thresholds</summary>
        public static readonly TimeSpan DatabaseQueryFast = TimeSpan.FromMilliseconds(100);
        public static readonly TimeSpan DatabaseQueryNormal = TimeSpan.FromMilliseconds(500);
        public static readonly TimeSpan DatabaseQuerySlow = TimeSpan.FromSeconds(2);
        public static readonly TimeSpan DatabaseQueryMax = TimeSpan.FromSeconds(5);

        /// <summary>E2E operation thresholds</summary>
        public static readonly TimeSpan E2ENavigationFast = TimeSpan.FromSeconds(2);
        public static readonly TimeSpan E2ENavigationNormal = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan E2ENavigationSlow = TimeSpan.FromSeconds(10);
        public static readonly TimeSpan E2ENavigationMax = TimeSpan.FromSeconds(30);

        /// <summary>Cache operation thresholds</summary>
        public static readonly TimeSpan CacheOperationFast = TimeSpan.FromMilliseconds(50);
        public static readonly TimeSpan CacheOperationNormal = TimeSpan.FromMilliseconds(200);
        public static readonly TimeSpan CacheOperationSlow = TimeSpan.FromMilliseconds(500);
        public static readonly TimeSpan CacheOperationMax = TimeSpan.FromSeconds(2);

        /// <summary>Aspire infrastructure thresholds</summary>
        public static readonly TimeSpan AspireHealthCheckFast = TimeSpan.FromSeconds(2);
        public static readonly TimeSpan AspireHealthCheckNormal = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan AspireHealthCheckSlow = TimeSpan.FromSeconds(10);
        public static readonly TimeSpan AspireHealthCheckMax = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Standardized logging configurations for consistent test output
    /// Ensures readable and searchable test logs across all environments
    /// </summary>
    public static class LoggingConfiguration
    {
        /// <summary>Standard log prefixes for different test types</summary>
        public const string UnitTestPrefix = "🧪 UNIT";
        public const string IntegrationTestPrefix = "🔗 INTEGRATION";
        public const string E2ETestPrefix = "🌐 E2E";
        public const string PerformanceTestPrefix = "⚡ PERFORMANCE";
        public const string SecurityTestPrefix = "🔐 SECURITY";

        /// <summary>Standard log prefixes for operations</summary>
        public const string SetupPrefix = "⚙️ SETUP";
        public const string CleanupPrefix = "🧹 CLEANUP";
        public const string ValidationPrefix = "✅ VALIDATION";
        public const string ErrorPrefix = "❌ ERROR";
        public const string WarningPrefix = "⚠️ WARNING";
        public const string InfoPrefix = "ℹ️ INFO";

        /// <summary>Standard timestamp format for consistent logging</summary>
        public const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
        public const string IsoTimestampFormat = "O";

        /// <summary>Standard audit ID format for traceability</summary>
        public static string FormatAuditId(Guid auditId) => $"[{auditId}]";

        /// <summary>Standard operation duration format</summary>
        public static string FormatDuration(TimeSpan duration) => $"{duration.TotalMilliseconds:F1}ms";
    }
}