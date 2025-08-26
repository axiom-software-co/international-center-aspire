using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace InternationalCenter.Tests.Shared.Utils;

/// <summary>
/// HTTP-specific contract validation utilities for Gateway and API integration tests
/// Extracts common HTTP validation patterns from duplicated Gateway contract tests
/// Provides standardized validation for HTTP contracts, security, and API responses
/// Medical-grade HTTP validation ensuring consistent gateway behavior
/// </summary>
public static class HttpContractValidationUtils
{
    #region Gateway Routing Contract Validation
    
    /// <summary>
    /// Validates that gateway properly routes requests to backend services
    /// Common pattern in Public and Admin Gateway contract tests
    /// </summary>
    public static async Task ValidateGatewayRouting(
        HttpClient client,
        string endpoint,
        HttpStatusCode expectedMinStatus = HttpStatusCode.OK,
        HttpStatusCode expectedMaxStatus = HttpStatusCode.PartialContent,
        ITestOutputHelper? output = null)
    {
        output?.WriteLine($"🔄 GATEWAY ROUTING: Testing {endpoint}");
        
        var response = await client.GetAsync(endpoint);
        
        // Validate routing succeeded (not a routing failure)
        var routingFailureStatuses = new[]
        {
            HttpStatusCode.NotFound,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.GatewayTimeout
        };
        
        if (routingFailureStatuses.Contains(response.StatusCode))
        {
            var message = $"Gateway routing failed for {endpoint}: {response.StatusCode}";
            output?.WriteLine($"❌ GATEWAY ROUTING: {message}");
            throw new InvalidOperationException($"Gateway routing contract violated: {message}");
        }
        
        // Validate status code is in expected range
        if (response.StatusCode < expectedMinStatus || response.StatusCode > expectedMaxStatus)
        {
            var message = $"Gateway response status {response.StatusCode} outside expected range {expectedMinStatus}-{expectedMaxStatus}";
            output?.WriteLine($"❌ GATEWAY ROUTING: {message}");
            throw new InvalidOperationException($"Gateway routing contract violated: {message}");
        }
        
        output?.WriteLine($"✅ GATEWAY ROUTING: Successfully routed {endpoint} -> {response.StatusCode}");
    }
    
    /// <summary>
    /// Validates multiple endpoint routing in batch for gateway contract testing
    /// </summary>
    public static async Task ValidateMultipleEndpointRouting(
        HttpClient client,
        string[] endpoints,
        ITestOutputHelper? output = null)
    {
        var failures = new List<string>();
        
        foreach (var endpoint in endpoints)
        {
            try
            {
                await ValidateGatewayRouting(client, endpoint, output: output);
            }
            catch (InvalidOperationException ex)
            {
                failures.Add($"{endpoint}: {ex.Message}");
            }
        }
        
        if (failures.Any())
        {
            var message = $"Multiple endpoint routing failures:\n{string.Join("\n", failures)}";
            output?.WriteLine($"❌ BATCH ROUTING: {message}");
            throw new InvalidOperationException($"Gateway batch routing contract violated: {message}");
        }
        
        output?.WriteLine($"✅ BATCH ROUTING: All {endpoints.Length} endpoints routed successfully");
    }
    
    #endregion
    
    #region API Response Contract Validation
    
    /// <summary>
    /// Validates API response structure and content for contract compliance
    /// Common pattern across Services API integration tests
    /// </summary>
    public static async Task<T> ValidateApiResponse<T>(
        HttpResponseMessage response,
        bool requireSuccessStatus = true,
        ITestOutputHelper? output = null)
    {
        if (requireSuccessStatus)
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var message = $"API response failed: {response.StatusCode} - {errorContent}";
                output?.WriteLine($"❌ API RESPONSE: {message}");
                throw new InvalidOperationException($"API response contract violated: {message}");
            }
        }
        
        // Validate content type
        var contentType = response.Content.Headers.ContentType?.MediaType;
        if (contentType != "application/json")
        {
            var message = $"Expected JSON response, got {contentType}";
            output?.WriteLine($"❌ API RESPONSE: {message}");
            throw new InvalidOperationException($"API response contract violated: {message}");
        }
        
        // Attempt to deserialize to expected type
        T? result;
        try
        {
            result = await response.Content.ReadFromJsonAsync<T>();
        }
        catch (JsonException ex)
        {
            var message = $"Failed to deserialize response to {typeof(T).Name}: {ex.Message}";
            output?.WriteLine($"❌ API RESPONSE: {message}");
            throw new InvalidOperationException($"API response contract violated: {message}");
        }
        
        if (result == null)
        {
            var message = $"API response deserialized to null for type {typeof(T).Name}";
            output?.WriteLine($"❌ API RESPONSE: {message}");
            throw new InvalidOperationException($"API response contract violated: {message}");
        }
        
        output?.WriteLine($"✅ API RESPONSE: Valid {typeof(T).Name} response with {response.StatusCode}");
        return result;
    }
    
    #endregion
    
    #region Authentication Contract Validation
    
    /// <summary>
    /// Validates that protected endpoints properly enforce authentication
    /// Used by Admin Gateway contract tests for medical-grade security
    /// </summary>
    public static async Task ValidateAuthenticationRequired(
        HttpClient clientWithoutAuth,
        string protectedEndpoint,
        ITestOutputHelper? output = null)
    {
        output?.WriteLine($"🔒 AUTH REQUIRED: Testing {protectedEndpoint}");
        
        var response = await clientWithoutAuth.GetAsync(protectedEndpoint);
        
        var authRequiredStatuses = new[]
        {
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden
        };
        
        if (!authRequiredStatuses.Contains(response.StatusCode))
        {
            var message = $"Protected endpoint {protectedEndpoint} allowed access without auth: {response.StatusCode}";
            output?.WriteLine($"❌ AUTH REQUIRED: {message}");
            throw new InvalidOperationException($"Authentication contract violated: {message}");
        }
        
        output?.WriteLine($"✅ AUTH REQUIRED: {protectedEndpoint} properly rejected unauthorized access with {response.StatusCode}");
    }
    
    /// <summary>
    /// Validates that authenticated requests are properly processed
    /// </summary>
    public static async Task ValidateAuthenticatedAccess(
        HttpClient authenticatedClient,
        string protectedEndpoint,
        ITestOutputHelper? output = null)
    {
        output?.WriteLine($"🔓 AUTH ACCESS: Testing {protectedEndpoint}");
        
        var response = await authenticatedClient.GetAsync(protectedEndpoint);
        
        // Should not be rejected for auth reasons
        var authRejectionStatuses = new[]
        {
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden
        };
        
        if (authRejectionStatuses.Contains(response.StatusCode))
        {
            var message = $"Authenticated request to {protectedEndpoint} was rejected: {response.StatusCode}";
            output?.WriteLine($"❌ AUTH ACCESS: {message}");
            throw new InvalidOperationException($"Authentication contract violated: {message}");
        }
        
        output?.WriteLine($"✅ AUTH ACCESS: {protectedEndpoint} properly processed authenticated request with {response.StatusCode}");
    }
    
    #endregion
    
    #region Rate Limiting Contract Validation
    
    /// <summary>
    /// Validates that rate limiting is properly enforced for gateway protection
    /// Medical-grade systems require proper rate limiting for security
    /// </summary>
    public static async Task ValidateRateLimiting(
        Func<Task<HttpResponseMessage>> requestFactory,
        int maxRequests,
        TimeSpan timeWindow,
        ITestOutputHelper? output = null)
    {
        output?.WriteLine($"🚦 RATE LIMITING: Testing {maxRequests} requests in {timeWindow.TotalSeconds}s");
        
        var responses = new List<HttpResponseMessage>();
        var startTime = DateTime.UtcNow;
        
        // Send requests up to the limit plus extra to trigger rate limiting
        for (int i = 0; i < maxRequests + 5; i++)
        {
            var response = await requestFactory();
            responses.Add(response);
            
            // Small delay to avoid overwhelming the system
            await Task.Delay(10);
        }
        
        // Check that rate limiting kicked in
        var rateLimitedCount = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        
        if (rateLimitedCount == 0)
        {
            var message = "No rate limiting observed despite exceeding request limit";
            output?.WriteLine($"❌ RATE LIMITING: {message}");
            throw new InvalidOperationException($"Rate limiting contract violated: {message}");
        }
        
        // Check that some requests were successful (not all blocked)
        var successfulCount = responses.Count(r => r.IsSuccessStatusCode);
        
        if (successfulCount < maxRequests / 2) // At least half should succeed
        {
            var message = $"Too few successful requests: {successfulCount} (expected at least {maxRequests / 2})";
            output?.WriteLine($"❌ RATE LIMITING: {message}");
            throw new InvalidOperationException($"Rate limiting contract violated: {message}");
        }
        
        output?.WriteLine($"✅ RATE LIMITING: Properly enforced - {successfulCount} successful, {rateLimitedCount} rate-limited");
        
        // Cleanup
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }
    
    #endregion
    
    #region Content Validation Contract Utilities
    
    /// <summary>
    /// Validates pagination contract in API responses
    /// Common pattern across Services API integration tests
    /// </summary>
    public static void ValidatePaginationContract<T>(
        IPagedResult<T> pagedResult,
        int requestedPage,
        int requestedPageSize,
        ITestOutputHelper? output = null)
    {
        Assert.NotNull(pagedResult);
        
        // Validate pagination metadata
        Assert.True(pagedResult.Page >= 1, "Page number should be 1-based");
        Assert.True(pagedResult.PageSize > 0, "Page size should be positive");
        Assert.True(pagedResult.TotalCount >= 0, "Total count should not be negative");
        Assert.True(pagedResult.TotalPages >= 0, "Total pages should not be negative");
        
        // Validate requested parameters match response
        Assert.Equal(requestedPage, pagedResult.Page);
        Assert.Equal(requestedPageSize, pagedResult.PageSize);
        
        // Validate items count doesn't exceed page size
        Assert.True(pagedResult.Items.Count <= pagedResult.PageSize, 
            $"Items count {pagedResult.Items.Count} exceeds page size {pagedResult.PageSize}");
        
        // Validate total pages calculation
        var expectedTotalPages = (int)Math.Ceiling((double)pagedResult.TotalCount / pagedResult.PageSize);
        Assert.Equal(expectedTotalPages, pagedResult.TotalPages);
        
        output?.WriteLine($"✅ PAGINATION CONTRACT: Valid pagination - Page {pagedResult.Page}/{pagedResult.TotalPages}, {pagedResult.Items.Count} items");
    }
    
    /// <summary>
    /// Validates search result contract structure
    /// </summary>
    public static void ValidateSearchResultContract<T>(
        ISearchResult<T> searchResult,
        string searchTerm,
        ITestOutputHelper? output = null)
    {
        Assert.NotNull(searchResult);
        
        // Validate search metadata
        Assert.NotNull(searchResult.SearchTerm);
        Assert.Equal(searchTerm, searchResult.SearchTerm);
        Assert.True(searchResult.TotalMatches >= 0, "Total matches should not be negative");
        Assert.True(searchResult.ExecutionTime >= TimeSpan.Zero, "Execution time should not be negative");
        
        // Validate results collection
        Assert.NotNull(searchResult.Results);
        Assert.True(searchResult.Results.Count <= searchResult.TotalMatches, 
            "Results count should not exceed total matches");
        
        output?.WriteLine($"✅ SEARCH CONTRACT: Found {searchResult.TotalMatches} matches for '{searchTerm}' in {searchResult.ExecutionTime.TotalMilliseconds}ms");
    }
    
    #endregion
}

/// <summary>
/// Interface for paginated results used in validation
/// </summary>
public interface IPagedResult<T>
{
    int Page { get; }
    int PageSize { get; }
    int TotalCount { get; }
    int TotalPages { get; }
    IReadOnlyList<T> Items { get; }
}

/// <summary>
/// Interface for search results used in validation
/// </summary>
public interface ISearchResult<T>
{
    string SearchTerm { get; }
    int TotalMatches { get; }
    TimeSpan ExecutionTime { get; }
    IReadOnlyList<T> Results { get; }
}