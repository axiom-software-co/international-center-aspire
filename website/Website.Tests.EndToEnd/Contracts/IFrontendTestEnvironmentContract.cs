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
public interface IFrontendTestEnvironmentContract<TTestContext>
    where TTestContext : class, ITestContext
{
    /// <summary>
    /// Sets up the frontend testing environment with browser automation and API mocking
    /// Contract: Must provide clean, isolated testing environment for Vue components and Pinia stores
    /// </summary>
    Task<TTestContext> SetupFrontendTestEnvironmentAsync(
        FrontendTestEnvironmentOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a frontend test operation with performance tracking and browser automation
    /// Contract: Must provide comprehensive error handling and browser lifecycle management
    /// </summary>
    Task<T> ExecuteFrontendTestAsync<T>(
        TTestContext context,
        Func<TTestContext, Task<T>> testOperation,
        string operationName,
        PerformanceThreshold? performanceThreshold = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates frontend test environment configuration and browser readiness
    /// Contract: Must validate browser automation setup and API mock configuration
    /// </summary>
    Task ValidateFrontendEnvironmentAsync(
        TTestContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up frontend test environment including browser instances and mock servers
    /// Contract: Must ensure complete cleanup of browser resources and mock services
    /// </summary>
    Task CleanupFrontendEnvironmentAsync(
        TTestContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Contract for Vue component testing
/// Provides specialized testing capabilities for Vue.js components
/// </summary>
public interface IVueComponentTestContract
{
    /// <summary>
    /// Tests Vue component rendering and props validation
    /// Contract: Must validate component rendering with various prop combinations
    /// </summary>
    Task TestVueComponentRenderingAsync<TComponent>(
        TComponent component,
        Dictionary<string, object> props,
        ComponentRenderingOptions? options = null,
        ITestOutputHelper? output = null)
        where TComponent : class;

    /// <summary>
    /// Tests Vue component event emission and handling
    /// Contract: Must validate proper event emission and payload structure
    /// </summary>
    Task TestVueComponentEventsAsync<TComponent>(
        TComponent component,
        string eventName,
        object? expectedPayload = null,
        ComponentEventOptions? options = null,
        ITestOutputHelper? output = null)
        where TComponent : class;

    /// <summary>
    /// Tests Vue component slot behavior
    /// Contract: Must validate slot content rendering and scoped slot functionality
    /// </summary>
    Task TestVueComponentSlotsAsync<TComponent>(
        TComponent component,
        Dictionary<string, string> slotContent,
        ComponentSlotOptions? options = null,
        ITestOutputHelper? output = null)
        where TComponent : class;

    /// <summary>
    /// Tests Vue component lifecycle hooks
    /// Contract: Must validate component lifecycle behavior and cleanup
    /// </summary>
    Task TestVueComponentLifecycleAsync<TComponent>(
        TComponent component,
        ComponentLifecycleOptions? options = null,
        ITestOutputHelper? output = null)
        where TComponent : class;

    /// <summary>
    /// Tests Vue component accessibility compliance
    /// Contract: Must validate WCAG compliance and keyboard navigation
    /// </summary>
    Task TestVueComponentAccessibilityAsync<TComponent>(
        TComponent component,
        AccessibilityValidationRules? rules = null,
        ITestOutputHelper? output = null)
        where TComponent : class;

    /// <summary>
    /// Tests Vue component performance characteristics
    /// Contract: Must validate rendering performance meets frontend thresholds
    /// </summary>
    Task TestVueComponentPerformanceAsync<TComponent>(
        TComponent component,
        PerformanceThreshold threshold,
        ITestOutputHelper? output = null)
        where TComponent : class;
}

/// <summary>
/// Contract for Pinia store testing
/// Provides specialized testing capabilities for Pinia state management
/// </summary>
public interface IPiniaStoreTestContract
{
    /// <summary>
    /// Tests Pinia store state initialization and default values
    /// Contract: Must validate initial state setup and type safety
    /// </summary>
    Task TestPiniaStoreInitializationAsync<TStore>(
        TStore store,
        object expectedInitialState,
        ITestOutputHelper? output = null)
        where TStore : class;

    /// <summary>
    /// Tests Pinia store actions and state mutations
    /// Contract: Must validate action execution and state change tracking
    /// </summary>
    Task TestPiniaStoreActionsAsync<TStore>(
        TStore store,
        string actionName,
        object? actionPayload = null,
        Func<TStore, bool>? stateValidator = null,
        ITestOutputHelper? output = null)
        where TStore : class;

    /// <summary>
    /// Tests Pinia store getters and computed values
    /// Contract: Must validate getter computation and reactivity
    /// </summary>
    Task TestPiniaStoreGettersAsync<TStore>(
        TStore store,
        string getterName,
        object expectedValue,
        ITestOutputHelper? output = null)
        where TStore : class;

    /// <summary>
    /// Tests Pinia store API integration and async actions
    /// Contract: Must validate API call handling and error state management
    /// </summary>
    Task TestPiniaStoreApiIntegrationAsync<TStore>(
        TStore store,
        string apiActionName,
        object? apiPayload = null,
        MockApiResponse? expectedResponse = null,
        ITestOutputHelper? output = null)
        where TStore : class;

    /// <summary>
    /// Tests Pinia store error handling and recovery
    /// Contract: Must validate error state management and user feedback
    /// </summary>
    Task TestPiniaStoreErrorHandlingAsync<TStore>(
        TStore store,
        string errorActionName,
        Exception expectedError,
        ITestOutputHelper? output = null)
        where TStore : class;

    /// <summary>
    /// Tests Pinia store persistence and hydration
    /// Contract: Must validate state persistence across browser sessions
    /// </summary>
    Task TestPiniaStorePersistenceAsync<TStore>(
        TStore store,
        PersistenceTestOptions? options = null,
        ITestOutputHelper? output = null)
        where TStore : class;
}

/// <summary>
/// Contract for API client testing against Public Gateway
/// Provides specialized testing for frontend-backend integration
/// </summary>
public interface IApiClientTestContract
{
    /// <summary>
    /// Tests API client request/response handling
    /// Contract: Must validate request formatting and response parsing
    /// </summary>
    Task TestApiClientRequestResponseAsync<TClient, TRequest, TResponse>(
        TClient apiClient,
        TRequest request,
        TResponse expectedResponse,
        ITestOutputHelper? output = null)
        where TClient : class
        where TRequest : class
        where TResponse : class;

    /// <summary>
    /// Tests API client error handling and retry logic
    /// Contract: Must validate error response handling and user-friendly error messages
    /// </summary>
    Task TestApiClientErrorHandlingAsync<TClient>(
        TClient apiClient,
        string endpointPath,
        HttpStatusCode expectedErrorCode,
        string expectedErrorPattern,
        ITestOutputHelper? output = null)
        where TClient : class;

    /// <summary>
    /// Tests API client authentication and authorization
    /// Contract: Must validate anonymous access patterns for Public Gateway
    /// </summary>
    Task TestApiClientAuthenticationAsync<TClient>(
        TClient apiClient,
        string endpointPath,
        bool expectsAnonymousAccess,
        ITestOutputHelper? output = null)
        where TClient : class;

    /// <summary>
    /// Tests API client performance and timeout handling
    /// Contract: Must validate request timeouts and performance thresholds
    /// </summary>
    Task TestApiClientPerformanceAsync<TClient>(
        TClient apiClient,
        string endpointPath,
        PerformanceThreshold threshold,
        ITestOutputHelper? output = null)
        where TClient : class;

    /// <summary>
    /// Tests API client caching behavior
    /// Contract: Must validate response caching and cache invalidation
    /// </summary>
    Task TestApiClientCachingAsync<TClient>(
        TClient apiClient,
        string endpointPath,
        CachingTestOptions? options = null,
        ITestOutputHelper? output = null)
        where TClient : class;

    /// <summary>
    /// Tests API client correlation tracking for audit trails
    /// Contract: Must validate correlation ID propagation for medical-grade audit requirements
    /// </summary>
    Task TestApiClientCorrelationTrackingAsync<TClient>(
        TClient apiClient,
        string endpointPath,
        CorrelationRequirements correlationRequirements,
        ITestOutputHelper? output = null)
        where TClient : class;
}

/// <summary>
/// Contract for browser automation testing with Playwright
/// Provides end-to-end testing capabilities for website functionality
/// </summary>
public interface IBrowserAutomationTestContract
{
    /// <summary>
    /// Tests browser navigation and page loading
    /// Contract: Must validate page accessibility and loading performance
    /// </summary>
    Task TestBrowserNavigationAsync(
        IPage page,
        string url,
        NavigationTestOptions? options = null,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests user interaction workflows
    /// Contract: Must validate user input handling and form submissions
    /// </summary>
    Task TestUserInteractionWorkflowAsync(
        IPage page,
        UserInteractionSequence interactionSequence,
        WorkflowValidationOptions? options = null,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests responsive design and mobile compatibility
    /// Contract: Must validate layout across different viewport sizes
    /// </summary>
    Task TestResponsiveDesignAsync(
        IPage page,
        ViewportSize[] viewportSizes,
        ResponsiveTestOptions? options = null,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests accessibility compliance with automated tools
    /// Contract: Must validate WCAG 2.1 AA compliance and keyboard navigation
    /// </summary>
    Task TestBrowserAccessibilityAsync(
        IPage page,
        AccessibilityValidationRules rules,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests browser performance metrics
    /// Contract: Must validate Core Web Vitals and loading performance
    /// </summary>
    Task TestBrowserPerformanceAsync(
        IPage page,
        PerformanceMetricsOptions? options = null,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests browser console errors and warnings
    /// Contract: Must validate absence of JavaScript errors and console warnings
    /// </summary>
    Task TestBrowserConsoleAsync(
        IPage page,
        ConsoleValidationOptions? options = null,
        ITestOutputHelper? output = null);
}

/// <summary>
/// Configuration options for frontend test environment setup
/// </summary>
public class FrontendTestEnvironmentOptions
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
    /// Gets or sets whether to enable API mocking (default: true)
    /// </summary>
    public bool EnableApiMocking { get; set; } = true;

    /// <summary>
    /// Gets or sets the Public Gateway base URL for API mocking
    /// </summary>
    public string? PublicGatewayBaseUrl { get; set; } = "http://localhost:5001";

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
    /// Gets or sets the viewport size for testing (default: 1280x720)
    /// </summary>
    public ViewportSize DefaultViewportSize { get; set; } = new(1280, 720);

    /// <summary>
    /// Gets or sets additional browser launch options
    /// </summary>
    public Dictionary<string, object> BrowserLaunchOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets custom service registrations
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
/// Options for component rendering tests
/// </summary>
public class ComponentRenderingOptions
{
    public bool ValidateHtml { get; set; } = true;
    public bool CheckAccessibility { get; set; } = true;
    public TimeSpan? RenderTimeout { get; set; } = TimeSpan.FromSeconds(5);
}

/// <summary>
/// Options for component event tests
/// </summary>
public class ComponentEventOptions
{
    public TimeSpan EventTimeout { get; set; } = TimeSpan.FromSeconds(2);
    public bool ValidatePayload { get; set; } = true;
    public bool AllowMultipleEvents { get; set; } = false;
}

/// <summary>
/// Options for component slot tests
/// </summary>
public class ComponentSlotOptions
{
    public bool ValidateSlotContent { get; set; } = true;
    public bool TestScopedSlots { get; set; } = true;
    public Dictionary<string, object>? SlotProps { get; set; }
}

/// <summary>
/// Options for component lifecycle tests
/// </summary>
public class ComponentLifecycleOptions
{
    public bool TestMounted { get; set; } = true;
    public bool TestUpdated { get; set; } = true;
    public bool TestUnmounted { get; set; } = true;
    public TimeSpan LifecycleTimeout { get; set; } = TimeSpan.FromSeconds(3);
}

/// <summary>
/// Mock API response for testing
/// </summary>
public class MockApiResponse
{
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
    public object? Data { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public TimeSpan? Delay { get; set; }
}

/// <summary>
/// Options for persistence testing
/// </summary>
public class PersistenceTestOptions
{
    public bool TestLocalStorage { get; set; } = true;
    public bool TestSessionStorage { get; set; } = true;
    public bool TestIndexedDB { get; set; } = false;
    public TimeSpan PersistenceTimeout { get; set; } = TimeSpan.FromSeconds(2);
}

/// <summary>
/// Options for caching tests
/// </summary>
public class CachingTestOptions
{
    public TimeSpan CacheExpiry { get; set; } = TimeSpan.FromMinutes(5);
    public bool TestCacheInvalidation { get; set; } = true;
    public bool TestCacheHitMiss { get; set; } = true;
}

/// <summary>
/// Options for navigation tests
/// </summary>
public class NavigationTestOptions
{
    public TimeSpan NavigationTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public bool WaitForNetworkIdle { get; set; } = true;
    public bool ValidatePageTitle { get; set; } = true;
    public string? ExpectedTitle { get; set; }
}

/// <summary>
/// User interaction sequence for workflow testing
/// </summary>
public class UserInteractionSequence
{
    public List<UserInteractionStep> Steps { get; set; } = new();
    public TimeSpan StepTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public bool ValidateEachStep { get; set; } = true;
}

/// <summary>
/// Individual user interaction step
/// </summary>
public class UserInteractionStep
{
    public UserInteractionType Type { get; set; }
    public string Selector { get; set; } = string.Empty;
    public string? Value { get; set; }
    public Dictionary<string, object>? Options { get; set; }
    public Func<IPage, Task<bool>>? Validator { get; set; }
}

/// <summary>
/// Types of user interactions
/// </summary>
public enum UserInteractionType
{
    Click,
    Type,
    Fill,
    SelectOption,
    Check,
    Uncheck,
    Hover,
    Focus,
    Blur,
    KeyPress,
    Scroll,
    Wait
}

/// <summary>
/// Options for workflow validation
/// </summary>
public class WorkflowValidationOptions
{
    public bool ValidateUrlChanges { get; set; } = true;
    public bool ValidatePageContent { get; set; } = true;
    public bool CaptureScreenshots { get; set; } = false;
    public string? ScreenshotPath { get; set; }
}

/// <summary>
/// Options for responsive design testing
/// </summary>
public class ResponsiveTestOptions
{
    public bool TestLayoutShift { get; set; } = true;
    public bool TestContentVisibility { get; set; } = true;
    public bool TestNavigationUsability { get; set; } = true;
    public bool CaptureScreenshots { get; set; } = false;
}

/// <summary>
/// Options for performance metrics testing
/// </summary>
public class PerformanceMetricsOptions
{
    public bool MeasureLCP { get; set; } = true; // Largest Contentful Paint
    public bool MeasureFID { get; set; } = true; // First Input Delay
    public bool MeasureCLS { get; set; } = true; // Cumulative Layout Shift
    public bool MeasureTTFB { get; set; } = true; // Time to First Byte
    public bool MeasureFCP { get; set; } = true; // First Contentful Paint
    public PerformanceThreshold? PerformanceThresholds { get; set; }
}

/// <summary>
/// Options for console validation
/// </summary>
public class ConsoleValidationOptions
{
    public bool AllowWarnings { get; set; } = true;
    public bool AllowErrors { get; set; } = false;
    public string[] IgnorePatterns { get; set; } = Array.Empty<string>();
    public ConsoleMessageType[] MonitoredTypes { get; set; } = { ConsoleMessageType.Error, ConsoleMessageType.Warning };
}

/// <summary>
/// Console message types to monitor
/// </summary>
public enum ConsoleMessageType
{
    Log,
    Info,
    Warning,
    Error,
    Debug
}

/// <summary>
/// Accessibility validation rules
/// </summary>
public class AccessibilityValidationRules
{
    public bool ValidateWCAG21AA { get; set; } = true;
    public bool ValidateKeyboardNavigation { get; set; } = true;
    public bool ValidateAriaLabels { get; set; } = true;
    public bool ValidateColorContrast { get; set; } = true;
    public bool ValidateHeadingStructure { get; set; } = true;
    public string[] IgnoreRules { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Context for Website domain testing
/// Provides Website-specific testing context and browser automation
/// </summary>
public interface IWebsiteTestContext : ITestContext
{
    /// <summary>
    /// Gets the frontend test service provider
    /// </summary>
    IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Gets the frontend test configuration
    /// </summary>
    IConfiguration Configuration { get; }

    /// <summary>
    /// Gets the frontend test logger
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
    /// Gets the API mock server instance
    /// </summary>
    IApiMockServer? ApiMockServer { get; }

    /// <summary>
    /// Gets the Public Gateway API client for testing
    /// </summary>
    IPublicGatewayApiClient? PublicGatewayClient { get; }

    /// <summary>
    /// Gets test entities created during this context
    /// </summary>
    ICollection<object> CreatedTestEntities { get; }

    /// <summary>
    /// Creates a new browser page for testing
    /// </summary>
    Task<IPage> CreatePageAsync(ViewportSize? viewportSize = null);

    /// <summary>
    /// Navigates to a URL and waits for page load
    /// </summary>
    Task NavigateToAsync(string url, NavigationTestOptions? options = null);

    /// <summary>
    /// Sets up API mocks for the current test
    /// </summary>
    Task SetupApiMocksAsync(Dictionary<string, MockApiResponse> mockResponses);

    /// <summary>
    /// Captures screenshot for debugging
    /// </summary>
    Task<string> CaptureScreenshotAsync(string? name = null);

    /// <summary>
    /// Registers an entity for cleanup after test completion
    /// </summary>
    void RegisterForCleanup<T>(T entity) where T : class;
}

/// <summary>
/// Interface for API mock server
/// </summary>
public interface IApiMockServer
{
    Task StartAsync();
    Task StopAsync();
    void SetupMock(string path, MockApiResponse response);
    void ClearMocks();
    string BaseUrl { get; }
}

/// <summary>
/// Interface for Public Gateway API client
/// </summary>
public interface IPublicGatewayApiClient
{
    Task<T> GetAsync<T>(string endpoint);
    Task<T> PostAsync<T>(string endpoint, object data);
    Task<T> PutAsync<T>(string endpoint, object data);
    Task DeleteAsync(string endpoint);
    string BaseUrl { get; }
}