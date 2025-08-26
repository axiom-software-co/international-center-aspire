using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using InternationalCenter.Shared.Tests.Abstractions;
using Xunit.Abstractions;

namespace InternationalCenter.Website.Shared.Tests.Contracts;

/// <summary>
/// Contract for Website domain testing environment
/// Defines comprehensive frontend testing capabilities with Vue components and Pinia stores
/// Medical-grade frontend testing environment ensuring anonymous access patterns and Public Gateway integration
/// </summary>
/// <typeparam name="TTestContext">The Website-specific test context type</typeparam>
public interface IWebsiteTestEnvironmentContract<TTestContext>
    where TTestContext : class, ITestContext
{
    /// <summary>
    /// Sets up the frontend testing environment with browser automation and API mocking
    /// Contract: Must provide clean, isolated testing environment for Vue components and Pinia stores in Websites/ folder
    /// </summary>
    Task<TTestContext> SetupWebsiteTestEnvironmentAsync(
        WebsiteTestEnvironmentOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a frontend test operation with performance tracking and browser automation
    /// Contract: Must provide comprehensive error handling and browser lifecycle management for anonymous user workflows
    /// </summary>
    Task<T> ExecuteWebsiteTestAsync<T>(
        TTestContext context,
        Func<TTestContext, Task<T>> testOperation,
        string operationName,
        PerformanceThreshold? performanceThreshold = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates frontend test environment configuration and browser readiness
    /// Contract: Must validate browser automation setup, Public Gateway API mock configuration, and anonymous access patterns
    /// </summary>
    Task ValidateWebsiteEnvironmentAsync(
        TTestContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up frontend test environment including browser instances and mock servers
    /// Contract: Must ensure complete cleanup of browser resources, API mock servers, and test data isolation
    /// </summary>
    Task CleanupWebsiteEnvironmentAsync(
        TTestContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Contract for Website test data factory
/// Provides realistic test data for frontend components and API mocking
/// </summary>
public interface IWebsiteTestDataFactoryContract
{
    /// <summary>
    /// Creates mock service data for component testing
    /// Contract: Must generate realistic service data matching Public Gateway API responses
    /// </summary>
    Task<ServiceTestData[]> CreateMockServiceDataAsync(
        int count = 10,
        Action<ServiceTestData>? configure = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates mock category data for component testing
    /// Contract: Must generate realistic category hierarchy data for frontend display
    /// </summary>
    Task<CategoryTestData[]> CreateMockCategoryDataAsync(
        int depth = 3,
        int breadth = 5,
        Action<CategoryTestData>? configure = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates mock API responses for MSW integration
    /// Contract: Must generate realistic API responses matching Public Gateway schemas
    /// </summary>
    Task<MockApiResponse[]> CreateMockApiResponsesAsync(
        string[] endpoints,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates mock user interaction data for E2E testing
    /// Contract: Must generate realistic user interaction sequences for anonymous users
    /// </summary>
    Task<UserInteractionSequence[]> CreateMockUserInteractionsAsync(
        string[] workflows,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates mock data quality and realism
    /// Contract: Must ensure mock data follows realistic patterns for frontend components
    /// </summary>
    Task ValidateMockDataQualityAsync<T>(
        T mockData,
        MockDataQualityRules<T>? qualityRules = null)
        where T : class;
}

/// <summary>
/// Contract for Website validation utilities
/// Provides specialized validation for frontend components and API integration
/// </summary>
public interface IWebsiteValidationUtilitiesContract
{
    /// <summary>
    /// Validates Vue component accessibility compliance
    /// Contract: Must validate WCAG 2.1 AA compliance and keyboard navigation for anonymous users
    /// </summary>
    Task ValidateComponentAccessibilityAsync<TComponent>(
        TComponent component,
        AccessibilityValidationRules rules,
        ITestOutputHelper? output = null)
        where TComponent : class;

    /// <summary>
    /// Validates Pinia store state management
    /// Contract: Must validate store state consistency, reactivity, and persistence for anonymous sessions
    /// </summary>
    Task ValidatePiniaStoreStateAsync<TStore>(
        TStore store,
        StoreStateValidationRules rules,
        ITestOutputHelper? output = null)
        where TStore : class;

    /// <summary>
    /// Validates Public Gateway API client integration
    /// Contract: Must validate API client behavior, error handling, and anonymous access patterns
    /// </summary>
    Task ValidateApiClientIntegrationAsync<TApiClient>(
        TApiClient apiClient,
        ApiClientValidationRules rules,
        ITestOutputHelper? output = null)
        where TApiClient : class;

    /// <summary>
    /// Validates browser performance and Core Web Vitals
    /// Contract: Must validate frontend performance meets medical-grade user experience requirements
    /// </summary>
    Task ValidateBrowserPerformanceAsync(
        IPage page,
        PerformanceValidationRules rules,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Validates responsive design and mobile compatibility
    /// Contract: Must validate layout and usability across different viewport sizes for accessibility compliance
    /// </summary>
    Task ValidateResponsiveDesignAsync(
        IPage page,
        ViewportSize[] viewportSizes,
        ResponsiveValidationRules rules,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Validates security headers and privacy protection
    /// Contract: Must validate security headers and data privacy for anonymous user protection
    /// </summary>
    Task ValidateSecurityPrivacyAsync(
        IPage page,
        SecurityPrivacyValidationRules rules,
        ITestOutputHelper? output = null);
}

/// <summary>
/// Configuration options for Website test environment setup
/// </summary>
public class WebsiteTestEnvironmentOptions
{
    /// <summary>
    /// Gets or sets whether to use headless browser mode (default: true)
    /// </summary>
    public bool UseHeadlessBrowser { get; set; } = true;

    /// <summary>
    /// Gets or sets the browser type to use for testing
    /// </summary>
    public BrowserType BrowserType { get; set; } = BrowserType.Chromium;

    /// <summary>
    /// Gets or sets whether to enable API mocking with MSW (default: true)
    /// </summary>
    public bool EnableApiMocking { get; set; } = true;

    /// <summary>
    /// Gets or sets the Public Gateway base URL for API mocking
    /// </summary>
    public string PublicGatewayBaseUrl { get; set; } = "http://localhost:5001";

    /// <summary>
    /// Gets or sets whether to enable performance tracking (default: true)
    /// </summary>
    public bool EnablePerformanceTracking { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable detailed logging (default: false)
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to enable accessibility validation (default: true)
    /// </summary>
    public bool EnableAccessibilityValidation { get; set; } = true;

    /// <summary>
    /// Gets or sets the default viewport size for testing (default: 1280x720)
    /// </summary>
    public ViewportSize DefaultViewportSize { get; set; } = new(1280, 720);

    /// <summary>
    /// Gets or sets additional browser launch options
    /// </summary>
    public Dictionary<string, object> BrowserLaunchOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets test-specific environment variables
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

    /// <summary>
    /// Gets or sets custom service registrations for DI
    /// </summary>
    public Action<IServiceCollection>? ConfigureServices { get; set; }
}

/// <summary>
/// Browser type enumeration for testing
/// </summary>
public enum BrowserType
{
    Chromium,
    Firefox,
    WebKit
}

/// <summary>
/// Viewport size for responsive testing
/// </summary>
public record ViewportSize(int Width, int Height);

/// <summary>
/// Quality rules for mock data validation
/// </summary>
public class MockDataQualityRules<T> where T : class
{
    public Func<T, bool> IsRealistic { get; set; } = _ => true;
    public Func<T, bool> MatchesApiSchema { get; set; } = _ => true;
    public Func<T, bool> HasRequiredFields { get; set; } = _ => true;
    public string[] ForbiddenPatterns { get; set; } = { "test", "mock", "fake", "lorem" };
}

/// <summary>
/// Validation rules for store state
/// </summary>
public class StoreStateValidationRules
{
    public bool ValidateReactivity { get; set; } = true;
    public bool ValidatePersistence { get; set; } = true;
    public bool ValidateStateConsistency { get; set; } = true;
    public bool ValidateErrorStates { get; set; } = true;
    public TimeSpan StateChangeTimeout { get; set; } = TimeSpan.FromSeconds(2);
}

/// <summary>
/// Validation rules for API client integration
/// </summary>
public class ApiClientValidationRules
{
    public bool ValidateAnonymousAccess { get; set; } = true;
    public bool ValidateErrorHandling { get; set; } = true;
    public bool ValidateRateLimitHandling { get; set; } = true;
    public bool ValidateCorrelationTracking { get; set; } = true;
    public bool ValidateResponseCaching { get; set; } = true;
    public TimeSpan MaxResponseTime { get; set; } = TimeSpan.FromSeconds(5);
}

/// <summary>
/// Validation rules for performance testing
/// </summary>
public class PerformanceValidationRules
{
    public bool ValidateLCP { get; set; } = true; // Largest Contentful Paint
    public bool ValidateFID { get; set; } = true; // First Input Delay
    public bool ValidateCLS { get; set; } = true; // Cumulative Layout Shift
    public bool ValidateTTFB { get; set; } = true; // Time to First Byte
    public bool ValidateFCP { get; set; } = true; // First Contentful Paint
    
    public TimeSpan MaxLCP { get; set; } = TimeSpan.FromMilliseconds(2500);
    public TimeSpan MaxFID { get; set; } = TimeSpan.FromMilliseconds(100);
    public double MaxCLS { get; set; } = 0.1;
    public TimeSpan MaxTTFB { get; set; } = TimeSpan.FromMilliseconds(600);
    public TimeSpan MaxFCP { get; set; } = TimeSpan.FromMilliseconds(1800);
}

/// <summary>
/// Validation rules for responsive design
/// </summary>
public class ResponsiveValidationRules
{
    public bool ValidateLayoutShift { get; set; } = true;
    public bool ValidateContentVisibility { get; set; } = true;
    public bool ValidateNavigationUsability { get; set; } = true;
    public bool ValidateTextReadability { get; set; } = true;
    public bool ValidateTouchTargets { get; set; } = true;
    public double MinTouchTargetSize { get; set; } = 44; // pixels
    public double MinTextSize { get; set; } = 16; // pixels
}

/// <summary>
/// Validation rules for security and privacy
/// </summary>
public class SecurityPrivacyValidationRules
{
    public bool ValidateSecurityHeaders { get; set; } = true;
    public bool ValidateHttpsUsage { get; set; } = true;
    public bool ValidateCookieSecure { get; set; } = true;
    public bool ValidateNoMixedContent { get; set; } = true;
    public bool ValidateNoSensitiveDataExposure { get; set; } = true;
    public string[] RequiredSecurityHeaders { get; set; } = {
        "Content-Security-Policy",
        "X-Content-Type-Options",
        "X-Frame-Options",
        "Strict-Transport-Security"
    };
}

/// <summary>
/// Context for Website domain testing
/// Provides Website-specific testing context and browser automation
/// </summary>
public interface IWebsiteTestContext : ITestContext
{
    /// <summary>
    /// Gets the website test service provider
    /// </summary>
    IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Gets the website test configuration
    /// </summary>
    IConfiguration Configuration { get; }

    /// <summary>
    /// Gets the website test logger
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// Gets the Playwright browser instance
    /// </summary>
    IBrowser? Browser { get; }

    /// <summary>
    /// Gets the current browser context
    /// </summary>
    IBrowserContext? BrowserContext { get; }

    /// <summary>
    /// Gets the current page instance
    /// </summary>
    IPage? Page { get; }

    /// <summary>
    /// Gets the API mock server instance for MSW integration
    /// </summary>
    IApiMockServer? ApiMockServer { get; }

    /// <summary>
    /// Gets the Public Gateway API client for testing
    /// </summary>
    IPublicGatewayApiClient? PublicGatewayClient { get; }

    /// <summary>
    /// Gets the Website test data factory
    /// </summary>
    IWebsiteTestDataFactoryContract TestDataFactory { get; }

    /// <summary>
    /// Gets the Website validation utilities
    /// </summary>
    IWebsiteValidationUtilitiesContract ValidationUtilities { get; }

    /// <summary>
    /// Gets test entities created during this context
    /// </summary>
    ICollection<object> CreatedTestEntities { get; }

    /// <summary>
    /// Creates a new browser page with specified viewport
    /// Contract: Must create isolated page instance for test execution
    /// </summary>
    Task<IPage> CreatePageAsync(ViewportSize? viewportSize = null);

    /// <summary>
    /// Navigates to a URL and waits for page load
    /// Contract: Must handle navigation timing and ensure page readiness for anonymous users
    /// </summary>
    Task NavigateToAsync(string url, NavigationOptions? options = null);

    /// <summary>
    /// Sets up API mocks for the current test
    /// Contract: Must configure MSW mocks for Public Gateway endpoints
    /// </summary>
    Task SetupApiMocksAsync(Dictionary<string, MockApiResponse> mockResponses);

    /// <summary>
    /// Captures screenshot for debugging and test evidence
    /// Contract: Must capture high-quality screenshots for test documentation
    /// </summary>
    Task<string> CaptureScreenshotAsync(string? name = null);

    /// <summary>
    /// Registers an entity for cleanup after test completion
    /// Contract: Must track entities for proper cleanup and test isolation
    /// </summary>
    void RegisterForCleanup<T>(T entity) where T : class;

    /// <summary>
    /// Gets or creates a cached test entity to avoid recreation
    /// Contract: Must provide entity caching for test performance optimization
    /// </summary>
    Task<T> GetOrCreateTestEntityAsync<T>(Func<Task<T>> factory) where T : class;
}

/// <summary>
/// Navigation options for page navigation
/// </summary>
public class NavigationOptions
{
    public TimeSpan NavigationTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public bool WaitForNetworkIdle { get; set; } = true;
    public bool ValidatePageTitle { get; set; } = true;
    public string? ExpectedTitle { get; set; }
    public bool CaptureScreenshot { get; set; } = false;
}

/// <summary>
/// Interface for API mock server using MSW
/// </summary>
public interface IApiMockServer
{
    Task StartAsync();
    Task StopAsync();
    void SetupMock(string path, MockApiResponse response);
    void ClearMocks();
    string BaseUrl { get; }
    bool IsRunning { get; }
}

/// <summary>
/// Interface for Public Gateway API client
/// </summary>
public interface IPublicGatewayApiClient
{
    Task<T> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default);
    Task<T> PostAsync<T>(string endpoint, object data, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> GetRawAsync(string endpoint, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> PostRawAsync(string endpoint, object data, CancellationToken cancellationToken = default);
    string BaseUrl { get; }
    TimeSpan DefaultTimeout { get; }
}

/// <summary>
/// Mock API response configuration
/// </summary>
public class MockApiResponse
{
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
    public object? Data { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public TimeSpan? Delay { get; set; }
    public int? CallCount { get; set; }
    public Func<object, bool>? RequestMatcher { get; set; }
}