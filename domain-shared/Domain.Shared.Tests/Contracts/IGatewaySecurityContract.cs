namespace InternationalCenter.Tests.Shared.Contracts;

/// <summary>
/// Contract interface for gateway security behavior testing
/// Ensures proper authentication, authorization, and security policy enforcement
/// Contract-first testing approach without knowledge of concrete implementations
/// Supports both Public Gateway (anonymous) and Admin Gateway (authenticated) patterns
/// </summary>
public interface IGatewaySecurityContract
{
    /// <summary>
    /// Contract test: Verify gateway enforces proper authentication requirements
    /// Public Gateway: Should allow anonymous access
    /// Admin Gateway: Should require valid authentication
    /// </summary>
    Task VerifySecurityContract_WithAuthenticationRequirement_EnforcesCorrectly();
    
    /// <summary>
    /// Contract test: Verify gateway rejects invalid authentication tokens
    /// </summary>
    Task VerifySecurityContract_WithInvalidToken_ReturnsUnauthorized();
    
    /// <summary>
    /// Contract test: Verify gateway adds required security headers
    /// </summary>
    Task VerifySecurityContract_WithAnyRequest_AddsSecurityHeaders();
    
    /// <summary>
    /// Contract test: Verify gateway enforces HTTPS redirection
    /// </summary>
    Task VerifySecurityContract_WithHttpRequest_EnforcesHttps();
    
    /// <summary>
    /// Contract test: Verify gateway applies CORS policy correctly
    /// </summary>
    Task VerifySecurityContract_WithCorsRequest_AppliesCorrectPolicy();
    
    /// <summary>
    /// Contract test: Verify gateway blocks suspicious requests (SQL injection, XSS)
    /// </summary>
    Task VerifySecurityContract_WithSuspiciousRequest_BlocksCorrectly();
    
    /// <summary>
    /// Contract test: Verify gateway enforces request size limits
    /// </summary>
    Task VerifySecurityContract_WithLargeRequest_EnforcesSizeLimit();
    
    /// <summary>
    /// Contract test: Verify gateway blocks requests from blacklisted IPs
    /// </summary>
    Task VerifySecurityContract_WithBlacklistedIp_BlocksAccess();
    
    /// <summary>
    /// Contract test: Verify gateway allows requests from whitelisted IPs
    /// </summary>
    Task VerifySecurityContract_WithWhitelistedIp_AllowsAccess();
    
    /// <summary>
    /// Contract test: Verify gateway detects and blocks suspicious User-Agent strings
    /// </summary>
    Task VerifySecurityContract_WithSuspiciousUserAgent_BlocksAccess();
}