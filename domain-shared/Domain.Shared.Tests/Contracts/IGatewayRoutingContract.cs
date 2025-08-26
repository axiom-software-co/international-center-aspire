namespace InternationalCenter.Tests.Shared.Contracts;

/// <summary>
/// Contract interface for gateway routing behavior testing
/// Ensures all gateways properly route requests to appropriate APIs
/// Contract-first testing approach without knowledge of concrete implementations
/// Focuses on Services Public and Admin APIs only (other APIs on hold per project rules)
/// </summary>
public interface IGatewayRoutingContract
{
    /// <summary>
    /// Contract test: Verify gateway accepts and routes valid Services API requests
    /// </summary>
    Task VerifyRoutingContract_WithServicesEndpoint_RoutesToCorrectApi();
    
    /// <summary>
    /// Contract test: Verify gateway returns proper 404 for non-existent endpoints
    /// </summary>
    Task VerifyRoutingContract_WithInvalidEndpoint_Returns404();
    
    /// <summary>
    /// Contract test: Verify gateway adds required identification headers
    /// </summary>
    Task VerifyRoutingContract_WithAnyRequest_AddsGatewayHeaders();
    
    /// <summary>
    /// Contract test: Verify gateway maintains correlation ID throughout request chain
    /// </summary>
    Task VerifyRoutingContract_WithCorrelationId_MaintainsTraceability();
    
    /// <summary>
    /// Contract test: Verify gateway handles HTTP methods correctly (GET, POST, PUT, DELETE)
    /// </summary>
    Task VerifyRoutingContract_WithDifferentHttpMethods_RoutesCorrectly();
    
    /// <summary>
    /// Contract test: Verify gateway preserves request body and query parameters
    /// </summary>
    Task VerifyRoutingContract_WithRequestBody_PreservesContent();
    
    /// <summary>
    /// Contract test: Verify gateway response headers are properly forwarded
    /// </summary>
    Task VerifyRoutingContract_WithApiResponse_ForwardsHeaders();
    
    /// <summary>
    /// Contract test: Verify gateway handles API failures gracefully
    /// </summary>
    Task VerifyRoutingContract_WithApiFailure_HandlesGracefully();
}