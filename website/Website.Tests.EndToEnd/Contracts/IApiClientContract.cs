using System.Net;
using InternationalCenter.Shared.Tests.Abstractions;
using Xunit.Abstractions;

namespace InternationalCenter.Website.Shared.Tests.Contracts;

/// <summary>
/// Contract for testing API clients that integrate with Public Gateway
/// Defines standardized tests for frontend-backend API communication
/// Medical-grade API client testing ensuring anonymous access patterns and security compliance
/// </summary>
/// <typeparam name="TApiClient">The API client type being tested</typeparam>
public interface IApiClientContract<TApiClient>
    where TApiClient : class
{
    /// <summary>
    /// Tests API client initialization and configuration
    /// Contract: Must validate proper base URL configuration and default headers setup
    /// </summary>
    Task TestApiClientInitializationAsync(
        TApiClient apiClient,
        ApiClientInitializationOptions options,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests API client request/response handling
    /// Contract: Must validate HTTP request construction and response parsing
    /// </summary>
    Task TestApiClientRequestResponseAsync<TRequest, TResponse>(
        TApiClient apiClient,
        string endpoint,
        HttpMethod method,
        TRequest? requestData,
        TResponse expectedResponse,
        ITestOutputHelper? output = null)
        where TRequest : class
        where TResponse : class;

    /// <summary>
    /// Tests API client error handling and status codes
    /// Contract: Must validate proper HTTP error handling and user-friendly error messages
    /// </summary>
    Task TestApiClientErrorHandlingAsync(
        TApiClient apiClient,
        string endpoint,
        HttpMethod method,
        HttpStatusCode expectedErrorCode,
        string expectedErrorPattern,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests API client anonymous access patterns
    /// Contract: Must validate proper anonymous access without authentication requirements
    /// </summary>
    Task TestApiClientAnonymousAccessAsync(
        TApiClient apiClient,
        string[] publicEndpoints,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests API client rate limiting handling
    /// Contract: Must validate proper rate limit response handling (1000 req/min IP-based)
    /// </summary>
    Task TestApiClientRateLimitingAsync(
        TApiClient apiClient,
        string endpoint,
        int requestCount,
        TimeSpan timeWindow,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests API client performance characteristics
    /// Contract: Must validate request performance meets frontend response time requirements
    /// </summary>
    Task TestApiClientPerformanceAsync(
        TApiClient apiClient,
        string endpoint,
        PerformanceThreshold threshold,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests API client correlation tracking for audit trails
    /// Contract: Must validate correlation ID propagation for medical-grade audit requirements
    /// </summary>
    Task TestApiClientCorrelationTrackingAsync(
        TApiClient apiClient,
        string endpoint,
        CorrelationRequirements correlationRequirements,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests API client caching behavior
    /// Contract: Must validate response caching strategies and cache invalidation
    /// </summary>
    Task TestApiClientCachingAsync(
        TApiClient apiClient,
        string endpoint,
        CachingTestOptions cachingOptions,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests API client timeout and retry logic
    /// Contract: Must validate proper timeout handling and retry strategies
    /// </summary>
    Task TestApiClientTimeoutRetryAsync(
        TApiClient apiClient,
        string endpoint,
        TimeoutRetryOptions options,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests API client security headers validation
    /// Contract: Must validate security headers are properly set and received
    /// </summary>
    Task TestApiClientSecurityHeadersAsync(
        TApiClient apiClient,
        string endpoint,
        SecurityHeaderRequirements securityRequirements,
        ITestOutputHelper? output = null);
}

/// <summary>
/// Contract for testing Public Gateway Services API client
/// Specialized contract for Services domain API operations through Public Gateway
/// </summary>
/// <typeparam name="TServicesApiClient">The services API client type being tested</typeparam>
public interface IPublicGatewayServicesApiClientContract<TServicesApiClient> : IApiClientContract<TServicesApiClient>
    where TServicesApiClient : class
{
    /// <summary>
    /// Tests service listing and pagination
    /// Contract: Must validate service list retrieval with proper pagination handling
    /// </summary>
    Task TestServiceListingAsync(
        TServicesApiClient apiClient,
        ServiceListingTestCase[] listingTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests service search functionality
    /// Contract: Must validate search operations with query parameters and filters
    /// </summary>
    Task TestServiceSearchAsync(
        TServicesApiClient apiClient,
        ServiceSearchTestCase[] searchTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests service details retrieval
    /// Contract: Must validate individual service detail fetching by ID
    /// </summary>
    Task TestServiceDetailsAsync(
        TServicesApiClient apiClient,
        ServiceDetailsTestCase[] detailTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests service filtering by categories
    /// Contract: Must validate category-based filtering with hierarchy support
    /// </summary>
    Task TestServiceCategoryFilteringAsync(
        TServicesApiClient apiClient,
        CategoryFilteringTestCase[] filteringTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests service location-based search
    /// Contract: Must validate geographical search with radius parameters
    /// </summary>
    Task TestServiceLocationSearchAsync(
        TServicesApiClient apiClient,
        LocationSearchTestCase[] locationTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests service data sanitization and privacy protection
    /// Contract: Must validate that sensitive information is not exposed in public endpoints
    /// </summary>
    Task TestServiceDataSanitizationAsync(
        TServicesApiClient apiClient,
        DataSanitizationTestCase[] sanitizationTestCases,
        ITestOutputHelper? output = null);
}

/// <summary>
/// Contract for testing Public Gateway Categories API client
/// Specialized contract for Categories domain API operations through Public Gateway
/// </summary>
/// <typeparam name="TCategoriesApiClient">The categories API client type being tested</typeparam>
public interface IPublicGatewayCategoriesApiClientContract<TCategoriesApiClient> : IApiClientContract<TCategoriesApiClient>
    where TCategoriesApiClient : class
{
    /// <summary>
    /// Tests category hierarchy retrieval
    /// Contract: Must validate category tree structure with parent-child relationships
    /// </summary>
    Task TestCategoryHierarchyAsync(
        TCategoriesApiClient apiClient,
        CategoryHierarchyTestCase[] hierarchyTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests category listing and filtering
    /// Contract: Must validate category list retrieval with optional filtering
    /// </summary>
    Task TestCategoryListingAsync(
        TCategoriesApiClient apiClient,
        CategoryListingTestCase[] listingTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests category details retrieval
    /// Contract: Must validate individual category detail fetching by ID
    /// </summary>
    Task TestCategoryDetailsAsync(
        TCategoriesApiClient apiClient,
        CategoryDetailsTestCase[] detailTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests category service associations
    /// Contract: Must validate service counts and associations for each category
    /// </summary>
    Task TestCategoryServiceAssociationsAsync(
        TCategoriesApiClient apiClient,
        CategoryServiceAssociationTestCase[] associationTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests category breadcrumb generation
    /// Contract: Must validate breadcrumb path generation for category navigation
    /// </summary>
    Task TestCategoryBreadcrumbGenerationAsync(
        TCategoriesApiClient apiClient,
        CategoryBreadcrumbTestCase[] breadcrumbTestCases,
        ITestOutputHelper? output = null);
}

/// <summary>
/// Contract for testing HTTP client factory and configuration
/// Validates HTTP client creation and configuration patterns
/// </summary>
/// <typeparam name="THttpClientFactory">The HTTP client factory type being tested</typeparam>
public interface IHttpClientFactoryContract<THttpClientFactory>
    where THttpClientFactory : class
{
    /// <summary>
    /// Tests HTTP client factory configuration
    /// Contract: Must validate proper client configuration and named client creation
    /// </summary>
    Task TestHttpClientFactoryConfigurationAsync(
        THttpClientFactory factory,
        HttpClientFactoryTestCase[] configurationTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests HTTP client lifecycle management
    /// Contract: Must validate proper client disposal and pooling behavior
    /// </summary>
    Task TestHttpClientLifecycleAsync(
        THttpClientFactory factory,
        HttpClientLifecycleOptions options,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests HTTP client middleware pipeline
    /// Contract: Must validate proper middleware registration and execution order
    /// </summary>
    Task TestHttpClientMiddlewarePipelineAsync(
        THttpClientFactory factory,
        MiddlewarePipelineTestCase[] middlewareTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests HTTP client error policies
    /// Contract: Must validate retry policies, circuit breakers, and timeout handling
    /// </summary>
    Task TestHttpClientErrorPoliciesAsync(
        THttpClientFactory factory,
        ErrorPolicyTestCase[] policyTestCases,
        ITestOutputHelper? output = null);
}

// Supporting classes and enums for API client testing

/// <summary>
/// Options for API client initialization testing
/// </summary>
public class ApiClientInitializationOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public Dictionary<string, string> DefaultHeaders { get; set; } = new();
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public bool ValidateConfiguration { get; set; } = true;
    public bool TestSslConfiguration { get; set; } = false;
}

/// <summary>
/// Options for timeout and retry testing
/// </summary>
public class TimeoutRetryOptions
{
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan CircuitBreakerThreshold { get; set; } = TimeSpan.FromSeconds(30);
    public bool TestTimeoutScenario { get; set; } = true;
    public bool TestRetryScenario { get; set; } = true;
}

/// <summary>
/// Test case for service listing
/// </summary>
public class ServiceListingTestCase
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
    public int ExpectedResultCount { get; set; }
    public bool TestPagination { get; set; } = true;
}

/// <summary>
/// Test case for service search
/// </summary>
public class ServiceSearchTestCase
{
    public string SearchQuery { get; set; } = string.Empty;
    public Dictionary<string, object>? SearchFilters { get; set; }
    public int ExpectedResultCount { get; set; }
    public bool TestFuzzySearch { get; set; } = true;
    public bool TestEmptyResults { get; set; } = false;
}

/// <summary>
/// Test case for service details
/// </summary>
public class ServiceDetailsTestCase
{
    public Guid ServiceId { get; set; } = Guid.NewGuid();
    public ServiceTestData? ExpectedServiceData { get; set; }
    public bool TestNotFound { get; set; } = false;
    public bool ValidateDataCompleteness { get; set; } = true;
}

/// <summary>
/// Test case for category filtering
/// </summary>
public class CategoryFilteringTestCase
{
    public Guid[] CategoryIds { get; set; } = Array.Empty<Guid>();
    public bool IncludeChildCategories { get; set; } = true;
    public int ExpectedResultCount { get; set; }
    public bool TestMultipleCategoryFilter { get; set; } = true;
}

/// <summary>
/// Test case for location search
/// </summary>
public class LocationSearchTestCase
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double RadiusKm { get; set; } = 10;
    public int ExpectedResultCount { get; set; }
    public bool TestProximityOrdering { get; set; } = true;
}

/// <summary>
/// Test case for data sanitization
/// </summary>
public class DataSanitizationTestCase
{
    public string[] SensitiveFields { get; set; } = Array.Empty<string>();
    public string[] RequiredPublicFields { get; set; } = Array.Empty<string>();
    public bool ValidateNoSensitiveData { get; set; } = true;
    public bool ValidateDataIntegrity { get; set; } = true;
}

/// <summary>
/// Test case for category listing
/// </summary>
public class CategoryListingTestCase
{
    public Guid? ParentCategoryId { get; set; }
    public bool IncludeServiceCounts { get; set; } = true;
    public int ExpectedCategoryCount { get; set; }
    public bool TestHierarchyStructure { get; set; } = true;
}

/// <summary>
/// Test case for category details
/// </summary>
public class CategoryDetailsTestCase
{
    public Guid CategoryId { get; set; } = Guid.NewGuid();
    public CategoryTestData? ExpectedCategoryData { get; set; }
    public bool TestNotFound { get; set; } = false;
    public bool ValidateServiceCount { get; set; } = true;
}

/// <summary>
/// Test case for category service associations
/// </summary>
public class CategoryServiceAssociationTestCase
{
    public Guid CategoryId { get; set; } = Guid.NewGuid();
    public int ExpectedServiceCount { get; set; }
    public bool TestServiceListing { get; set; } = true;
    public bool TestServicePagination { get; set; } = true;
}

/// <summary>
/// Test case for HTTP client factory configuration
/// </summary>
public class HttpClientFactoryTestCase
{
    public string ClientName { get; set; } = string.Empty;
    public string BaseAddress { get; set; } = string.Empty;
    public Dictionary<string, string> DefaultHeaders { get; set; } = new();
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool ValidateConfiguration { get; set; } = true;
}

/// <summary>
/// Options for HTTP client lifecycle testing
/// </summary>
public class HttpClientLifecycleOptions
{
    public int ClientCreationCount { get; set; } = 10;
    public TimeSpan ClientLifetime { get; set; } = TimeSpan.FromMinutes(2);
    public bool TestPooling { get; set; } = true;
    public bool TestDisposal { get; set; } = true;
    public bool ValidateMemoryUsage { get; set; } = true;
}

/// <summary>
/// Test case for middleware pipeline
/// </summary>
public class MiddlewarePipelineTestCase
{
    public string[] ExpectedMiddleware { get; set; } = Array.Empty<string>();
    public Dictionary<string, object>? MiddlewareConfiguration { get; set; }
    public bool TestExecutionOrder { get; set; } = true;
    public bool TestMiddlewareInjection { get; set; } = true;
}

/// <summary>
/// Test case for error policies
/// </summary>
public class ErrorPolicyTestCase
{
    public string PolicyName { get; set; } = string.Empty;
    public HttpStatusCode[] RetryableStatusCodes { get; set; } = Array.Empty<HttpStatusCode>();
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    public bool TestCircuitBreaker { get; set; } = false;
    public bool TestBulkhead { get; set; } = false;
}

/// <summary>
/// Response model for API testing
/// </summary>
public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public string? CorrelationId { get; set; }
}

/// <summary>
/// Paginated response model for API testing
/// </summary>
public class PaginatedApiResponse<T> : ApiResponse<IEnumerable<T>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

/// <summary>
/// Search response model for API testing
/// </summary>
public class SearchApiResponse<T> : PaginatedApiResponse<T>
{
    public string SearchQuery { get; set; } = string.Empty;
    public Dictionary<string, object>? AppliedFilters { get; set; }
    public TimeSpan SearchDuration { get; set; }
    public string[]? Suggestions { get; set; }
    public Dictionary<string, int>? Facets { get; set; }
}

/// <summary>
/// Error response model for API testing
/// </summary>
public class ApiErrorResponse
{
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string? ErrorDetails { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object>? ValidationErrors { get; set; }
}

/// <summary>
/// Rate limit response model for API testing
/// </summary>
public class RateLimitResponse
{
    public int RemainingRequests { get; set; }
    public TimeSpan ResetTime { get; set; }
    public int RequestLimit { get; set; }
    public TimeSpan WindowDuration { get; set; }
    public string? ClientIdentifier { get; set; }
}