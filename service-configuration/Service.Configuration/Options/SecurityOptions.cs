using FluentValidation;

namespace Service.Configuration.Options;

/// <summary>
/// Security configuration options for Services APIs and Gateways.
/// MEDICAL COMPLIANCE: Enhanced security requirements for medical data
/// FALLBACK POLICIES: Required when no specific policy matches
/// </summary>
public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    /// <summary>
    /// Enable medical-grade security policies
    /// MEDICAL COMPLIANCE: Enhanced security for medical data handling
    /// </summary>
    public bool EnableMedicalGradeCompliance { get; init; } = true;

    /// <summary>
    /// JWT token configuration for authentication
    /// ADMIN GATEWAY: Required for role-based access control
    /// </summary>
    public JwtOptions Jwt { get; init; } = new();

    /// <summary>
    /// CORS configuration for cross-origin requests
    /// PUBLIC GATEWAY: Allows public website origins
    /// </summary>
    public CorsOptions Cors { get; init; } = new();

    /// <summary>
    /// Security headers configuration
    /// MEDICAL COMPLIANCE: Security headers required for medical applications
    /// </summary>
    public SecurityHeadersOptions Headers { get; init; } = new();

    /// <summary>
    /// Rate limiting configuration
    /// PUBLIC GATEWAY: 1000 req/min, ADMIN GATEWAY: 100 req/min
    /// </summary>
    public RateLimitingOptions RateLimiting { get; init; } = new();

    /// <summary>
    /// Remove Kestrel server header from responses
    /// SECURITY: Prevents server fingerprinting
    /// </summary>
    public bool RemoveServerHeader { get; init; } = true;

    /// <summary>
    /// Enable fallback security policies
    /// MEDICAL COMPLIANCE: Fallback policies must exist when no specific policy matches
    /// </summary>
    public bool EnableFallbackPolicies { get; init; } = true;

    /// <summary>
    /// Fallback policy configuration
    /// SECURITY: Default deny behavior for unauthorized access
    /// </summary>
    public FallbackPolicyOptions FallbackPolicy { get; init; } = new();
}

/// <summary>
/// JWT token configuration for authentication and authorization.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>JWT token issuer URL</summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>JWT token audience</summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>JWT token validation parameters</summary>
    public JwtValidationOptions Validation { get; init; } = new();

    /// <summary>Token expiration time in minutes</summary>
    public int ExpirationMinutes { get; init; } = 60;

    /// <summary>Clock skew tolerance in minutes</summary>
    public int ClockSkewMinutes { get; init; } = 5;
}

/// <summary>
/// JWT token validation configuration.
/// </summary>
public sealed class JwtValidationOptions
{
    /// <summary>Validate token issuer</summary>
    public bool ValidateIssuer { get; init; } = true;

    /// <summary>Validate token audience</summary>
    public bool ValidateAudience { get; init; } = true;

    /// <summary>Validate token lifetime</summary>
    public bool ValidateLifetime { get; init; } = true;

    /// <summary>Validate issuer signing key</summary>
    public bool ValidateIssuerSigningKey { get; init; } = true;

    /// <summary>Require HTTPS for token endpoints</summary>
    public bool RequireHttpsMetadata { get; init; } = true;
}

/// <summary>
/// CORS configuration for cross-origin request handling.
/// </summary>
public sealed class CorsOptions
{
    /// <summary>Allowed origins for CORS requests</summary>
    public IReadOnlyList<string> AllowedOrigins { get; init; } = 
    [
        "http://localhost:4321",  // Astro dev server
        "http://localhost:5000",  // .NET Website
        "http://localhost:5001"   // .NET Website HTTPS
    ];

    /// <summary>Allowed HTTP methods</summary>
    public IReadOnlyList<string> AllowedMethods { get; init; } = 
    [
        "GET", "POST", "PUT", "DELETE", "OPTIONS", "HEAD"
    ];

    /// <summary>Allowed request headers</summary>
    public IReadOnlyList<string> AllowedHeaders { get; init; } = 
    [
        "Content-Type", "Authorization", "X-Requested-With", "X-Correlation-ID"
    ];

    /// <summary>Allow credentials in CORS requests</summary>
    public bool AllowCredentials { get; init; } = false;

    /// <summary>Preflight cache duration in seconds</summary>
    public int PreflightMaxAgeSeconds { get; init; } = 600; // 10 minutes
}

/// <summary>
/// Security headers configuration for medical-grade compliance.
/// </summary>
public sealed class SecurityHeadersOptions
{
    /// <summary>Enable Strict-Transport-Security header</summary>
    public bool EnableHsts { get; init; } = true;

    /// <summary>HSTS max age in seconds</summary>
    public int HstsMaxAgeSeconds { get; init; } = 31536000; // 1 year

    /// <summary>Enable X-Content-Type-Options: nosniff header</summary>
    public bool EnableContentTypeOptions { get; init; } = true;

    /// <summary>Enable X-Frame-Options header</summary>
    public bool EnableFrameOptions { get; init; } = true;

    /// <summary>X-Frame-Options value</summary>
    public string FrameOptionsValue { get; init; } = "DENY";

    /// <summary>Enable Content-Security-Policy header</summary>
    public bool EnableContentSecurityPolicy { get; init; } = true;

    /// <summary>Content Security Policy directives</summary>
    public string ContentSecurityPolicy { get; init; } = 
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'";

    /// <summary>Enable Referrer-Policy header</summary>
    public bool EnableReferrerPolicy { get; init; } = true;

    /// <summary>Referrer policy value</summary>
    public string ReferrerPolicyValue { get; init; } = "strict-origin-when-cross-origin";
}

/// <summary>
/// Rate limiting configuration for API protection.
/// </summary>
public sealed class RateLimitingOptions
{
    /// <summary>Public Gateway rate limit (requests per minute)</summary>
    public int PublicGatewayRateLimit { get; init; } = 1000;

    /// <summary>Admin Gateway rate limit (requests per minute)</summary>
    public int AdminGatewayRateLimit { get; init; } = 100;

    /// <summary>Rate limiting window duration in minutes</summary>
    public int WindowDurationMinutes { get; init; } = 1;

    /// <summary>IP-based rate limiting for anonymous users</summary>
    public bool EnableIpBasedLimiting { get; init; } = true;

    /// <summary>User-based rate limiting for authenticated users</summary>
    public bool EnableUserBasedLimiting { get; init; } = true;

    /// <summary>Rate limit response status code</summary>
    public int RateLimitStatusCode { get; init; } = 429;

    /// <summary>Rate limit response message</summary>
    public string RateLimitMessage { get; init; } = "Rate limit exceeded. Please try again later.";
}

/// <summary>
/// Fallback security policy configuration for medical compliance.
/// </summary>
public sealed class FallbackPolicyOptions
{
    /// <summary>Default action when no policy matches</summary>
    public FallbackAction DefaultAction { get; init; } = FallbackAction.Deny;

    /// <summary>Log all fallback policy evaluations</summary>
    public bool LogFallbackEvaluations { get; init; } = true;

    /// <summary>Enable medical audit for security policy failures</summary>
    public bool EnableMedicalAuditForFailures { get; init; } = true;

    /// <summary>Fallback policy response message</summary>
    public string FallbackMessage { get; init; } = "Access denied by fallback security policy.";
}

/// <summary>
/// Fallback actions for security policy evaluation.
/// </summary>
public enum FallbackAction
{
    /// <summary>Deny access (secure default)</summary>
    Deny = 0,
    
    /// <summary>Allow access (use with caution)</summary>
    Allow = 1,
    
    /// <summary>Require explicit authorization</summary>
    RequireAuth = 2
}

/// <summary>
/// FluentValidation validator for SecurityOptions.
/// MEDICAL COMPLIANCE: Ensures security configuration meets medical standards
/// </summary>
public sealed class SecurityOptionsValidator : AbstractValidator<SecurityOptions>
{
    public SecurityOptionsValidator()
    {
        RuleFor(x => x.Jwt.Issuer)
            .NotEmpty()
            .When(x => x.EnableMedicalGradeCompliance)
            .WithMessage("JWT issuer is required for medical-grade compliance");

        RuleFor(x => x.Jwt.Audience)
            .NotEmpty()
            .When(x => x.EnableMedicalGradeCompliance)
            .WithMessage("JWT audience is required for medical-grade compliance");

        RuleFor(x => x.Jwt.ExpirationMinutes)
            .GreaterThan(0)
            .LessThanOrEqualTo(1440) // 24 hours max
            .WithMessage("JWT expiration must be between 1 and 1440 minutes");

        RuleFor(x => x.Cors.AllowedOrigins)
            .NotEmpty()
            .WithMessage("At least one CORS origin must be configured");

        RuleFor(x => x.Headers.HstsMaxAgeSeconds)
            .GreaterThan(0)
            .LessThanOrEqualTo(63072000) // 2 years max
            .WithMessage("HSTS max age must be between 1 second and 2 years");

        RuleFor(x => x.RateLimiting.PublicGatewayRateLimit)
            .GreaterThan(0)
            .LessThanOrEqualTo(10000)
            .WithMessage("Public gateway rate limit must be between 1 and 10000 requests per minute");

        RuleFor(x => x.RateLimiting.AdminGatewayRateLimit)
            .GreaterThan(0)
            .LessThanOrEqualTo(1000)
            .WithMessage("Admin gateway rate limit must be between 1 and 1000 requests per minute");

        RuleFor(x => x.RateLimiting.WindowDurationMinutes)
            .GreaterThan(0)
            .LessThanOrEqualTo(60)
            .WithMessage("Rate limiting window must be between 1 and 60 minutes");

        // Medical compliance validations
        RuleFor(x => x)
            .Must(x => x.EnableMedicalGradeCompliance)
            .WithMessage("Medical-grade compliance must be enabled")
            .Must(x => x.EnableFallbackPolicies)
            .WithMessage("Fallback policies must be enabled for medical compliance")
            .Must(x => x.Headers.EnableHsts)
            .When(x => x.EnableMedicalGradeCompliance)
            .WithMessage("HSTS must be enabled for medical-grade compliance")
            .Must(x => x.Headers.EnableContentSecurityPolicy)
            .When(x => x.EnableMedicalGradeCompliance)
            .WithMessage("Content Security Policy must be enabled for medical-grade compliance");
    }
}