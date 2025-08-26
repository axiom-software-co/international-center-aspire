using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Service.Logging.Options;

/// <summary>
/// Structured logging configuration options for Services APIs with medical-grade compliance.
/// MEDICAL COMPLIANCE: Structured templates and enrichment for medical audit requirements
/// SERVICES APIS SCOPE: Logging patterns specific to Services Public/Admin APIs
/// </summary>
public sealed class StructuredLoggingOptions
{
    public const string SectionName = "StructuredLogging";

    /// <summary>
    /// Enable structured logging with JSON formatting
    /// MEDICAL COMPLIANCE: Structured format required for medical audit trails
    /// </summary>
    public bool EnableStructuredFormat { get; init; } = true;

    /// <summary>
    /// Message templates for structured logging across Services APIs
    /// SERVICES APIS: Pre-defined templates for Public/Admin API request/response patterns
    /// </summary>
    public MessageTemplatesOptions MessageTemplates { get; init; } = new();

    /// <summary>
    /// Enrichment configuration for additional structured data
    /// MEDICAL COMPLIANCE: Environment, process, and correlation enrichment
    /// </summary>
    public EnrichmentOptions Enrichment { get; init; } = new();

    /// <summary>
    /// Sensitive data redaction configuration
    /// MEDICAL COMPLIANCE: PII/PHI protection in log entries
    /// </summary>
    public SensitiveDataRedactionOptions SensitiveDataRedaction { get; init; } = new();

    /// <summary>
    /// Correlation ID configuration for request tracing
    /// SERVICES APIS: Request correlation across Public Gateway -> Services APIs
    /// </summary>
    public CorrelationIdOptions CorrelationId { get; init; } = new();

    /// <summary>
    /// Performance logging configuration for Services APIs
    /// MONITORING: Request timing and performance metrics
    /// </summary>
    public PerformanceLoggingOptions PerformanceLogging { get; init; } = new();
}

/// <summary>
/// Pre-defined message templates for structured logging across Services APIs.
/// SERVICES APIS: Standardized templates for Public/Admin API patterns
/// </summary>
public sealed class MessageTemplatesOptions
{
    /// <summary>HTTP request template for Services APIs</summary>
    public string HttpRequest { get; init; } = 
        "HTTP {RequestMethod} {RequestPath} started with correlation {CorrelationId}";

    /// <summary>HTTP response template for Services APIs</summary>
    public string HttpResponse { get; init; } = 
        "HTTP {RequestMethod} {RequestPath} completed in {ElapsedMs}ms with status {StatusCode} and correlation {CorrelationId}";

    /// <summary>Database query template with performance tracking</summary>
    public string DatabaseQuery { get; init; } = 
        "Database {QueryType} executed in {ElapsedMs}ms for correlation {CorrelationId}: {QuerySummary}";

    /// <summary>Cache operation template for Redis interactions</summary>
    public string CacheOperation { get; init; } = 
        "Cache {Operation} for key {CacheKey} completed in {ElapsedMs}ms with result {Result} and correlation {CorrelationId}";

    /// <summary>Authentication template for Admin APIs</summary>
    public string Authentication { get; init; } = 
        "Authentication {AuthResult} for user {UserId} with method {AuthMethod} and correlation {CorrelationId}";

    /// <summary>Authorization template for Admin APIs</summary>
    public string Authorization { get; init; } = 
        "Authorization {AuthzResult} for user {UserId} accessing {Resource} with policy {Policy} and correlation {CorrelationId}";

    /// <summary>Rate limiting template for Public Gateway</summary>
    public string RateLimiting { get; init; } = 
        "Rate limiting {Result} for client {ClientId} with {RequestCount}/{Limit} requests and correlation {CorrelationId}";

    /// <summary>Medical compliance audit template</summary>
    public string MedicalAudit { get; init; } = 
        "Medical audit {AuditAction} for entity {EntityType}:{EntityId} by user {UserId} with correlation {CorrelationId}";

    /// <summary>Error template with exception details</summary>
    public string Error { get; init; } = 
        "Error occurred in {SourceContext} with correlation {CorrelationId}: {ErrorMessage}";

    /// <summary>Critical error template for immediate attention</summary>
    public string Critical { get; init; } = 
        "CRITICAL ERROR in {SourceContext} with correlation {CorrelationId}: {ErrorMessage} - Immediate attention required";
}

/// <summary>
/// Enrichment configuration for structured logging enhancement.
/// MEDICAL COMPLIANCE: Additional context for audit trail completeness
/// </summary>
public sealed class EnrichmentOptions
{
    /// <summary>Enrich with environment name</summary>
    public bool WithEnvironment { get; init; } = true;

    /// <summary>Enrich with machine name</summary>
    public bool WithMachineName { get; init; } = true;

    /// <summary>Enrich with process ID</summary>
    public bool WithProcessId { get; init; } = true;

    /// <summary>Enrich with thread ID</summary>
    public bool WithThreadId { get; init; } = true;

    /// <summary>Enrich with user ID for authenticated requests</summary>
    public bool WithUserId { get; init; } = true;

    /// <summary>Enrich with request IP address</summary>
    public bool WithClientIpAddress { get; init; } = true;

    /// <summary>Enrich with user agent for request context</summary>
    public bool WithUserAgent { get; init; } = false; // Disabled by default for privacy

    /// <summary>Enrich with application version</summary>
    public bool WithApplicationVersion { get; init; } = true;

    /// <summary>Medical compliance enrichment properties</summary>
    public MedicalComplianceEnrichmentOptions MedicalCompliance { get; init; } = new();
}

/// <summary>
/// Medical compliance specific enrichment options.
/// MEDICAL COMPLIANCE: Additional audit trail context for medical standards
/// </summary>
public sealed class MedicalComplianceEnrichmentOptions
{
    /// <summary>Include data classification in logs</summary>
    public bool WithDataClassification { get; init; } = true;

    /// <summary>Include security context for audit</summary>
    public bool WithSecurityContext { get; init; } = true;

    /// <summary>Include compliance flags for audit</summary>
    public bool WithComplianceFlags { get; init; } = true;

    /// <summary>Include audit trail metadata</summary>
    public bool WithAuditMetadata { get; init; } = true;
}

/// <summary>
/// Sensitive data redaction configuration for medical compliance.
/// MEDICAL COMPLIANCE: PII/PHI protection in structured logs
/// </summary>
public sealed class SensitiveDataRedactionOptions
{
    /// <summary>Enable sensitive data redaction</summary>
    public bool EnableRedaction { get; init; } = true;

    /// <summary>Redaction placeholder text</summary>
    public string RedactionPlaceholder { get; init; } = "[REDACTED]";

    /// <summary>Property names to redact (case-insensitive)</summary>
    public IReadOnlyList<string> SensitivePropertyNames { get; init; } =
    [
        "password", "secret", "token", "key", "authorization",
        "ssn", "social", "passport", "license", "account",
        "credit", "card", "payment", "medical", "health",
        "patient", "diagnosis", "treatment", "medication",
        "phi", "pii", "sensitive", "private", "confidential"
    ];

    /// <summary>URL path patterns to redact</summary>
    public IReadOnlyList<string> SensitiveUrlPatterns { get; init; } =
    [
        "/api/*/password*",
        "/api/*/secret*",
        "/api/*/auth*",
        "/api/*/patient/*/medical*",
        "/api/*/health*"
    ];

    /// <summary>Regex patterns for sensitive data detection</summary>
    public IReadOnlyList<string> SensitiveDataPatterns { get; init; } =
    [
        @"\b\d{3}-\d{2}-\d{4}\b",           // SSN pattern
        @"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b", // Credit card pattern
        @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b" // Email pattern (optional)
    ];

    /// <summary>Maximum length for redacted values before truncation</summary>
    public int MaxRedactedLength { get; init; } = 50;
}

/// <summary>
/// Correlation ID configuration for request tracing across Services APIs.
/// SERVICES APIS: Request correlation from Public Gateway through Services APIs
/// </summary>
public sealed class CorrelationIdOptions
{
    /// <summary>Enable correlation ID generation and propagation</summary>
    public bool EnableCorrelationId { get; init; } = true;

    /// <summary>HTTP header name for correlation ID</summary>
    public string HeaderName { get; init; } = "X-Correlation-ID";

    /// <summary>Generate new correlation ID if not present</summary>
    public bool GenerateIfMissing { get; init; } = true;

    /// <summary>Include correlation ID in response headers</summary>
    public bool IncludeInResponseHeaders { get; init; } = true;

    /// <summary>Correlation ID format (Guid, ShortGuid, or Custom)</summary>
    public CorrelationIdFormat Format { get; init; } = CorrelationIdFormat.Guid;

    /// <summary>Custom correlation ID prefix</summary>
    public string CustomPrefix { get; init; } = "ICMR";

    /// <summary>Validate correlation ID format</summary>
    public bool ValidateFormat { get; init; } = true;
}

/// <summary>
/// Performance logging configuration for Services APIs monitoring.
/// MONITORING: Request performance and timing analysis
/// </summary>
public sealed class PerformanceLoggingOptions
{
    /// <summary>Enable performance logging</summary>
    public bool EnablePerformanceLogging { get; init; } = true;

    /// <summary>Slow request threshold in milliseconds</summary>
    public int SlowRequestThresholdMs { get; init; } = 1000;

    /// <summary>Log all requests regardless of performance</summary>
    public bool LogAllRequests { get; init; } = false;

    /// <summary>Include memory usage in performance logs</summary>
    public bool IncludeMemoryUsage { get; init; } = false;

    /// <summary>Include garbage collection metrics</summary>
    public bool IncludeGcMetrics { get; init; } = false;

    /// <summary>Performance metrics collection interval in seconds</summary>
    public int MetricsCollectionIntervalSeconds { get; init; } = 30;
}

/// <summary>
/// Correlation ID format options for request tracing.
/// </summary>
public enum CorrelationIdFormat
{
    /// <summary>Standard GUID format</summary>
    Guid = 0,
    
    /// <summary>Short GUID format (22 characters)</summary>
    ShortGuid = 1,
    
    /// <summary>Custom format with prefix</summary>
    Custom = 2
}

/// <summary>
/// FluentValidation validator for StructuredLoggingOptions.
/// MEDICAL COMPLIANCE: Ensures logging configuration meets medical standards
/// </summary>
public sealed class StructuredLoggingOptionsValidator : AbstractValidator<StructuredLoggingOptions>
{
    public StructuredLoggingOptionsValidator()
    {
        RuleFor(x => x.MessageTemplates.HttpRequest)
            .NotEmpty()
            .WithMessage("HTTP request message template cannot be empty");

        RuleFor(x => x.MessageTemplates.HttpResponse)
            .NotEmpty()
            .WithMessage("HTTP response message template cannot be empty");

        RuleFor(x => x.SensitiveDataRedaction.RedactionPlaceholder)
            .NotEmpty()
            .WithMessage("Redaction placeholder cannot be empty");

        RuleFor(x => x.SensitiveDataRedaction.MaxRedactedLength)
            .GreaterThan(0)
            .LessThanOrEqualTo(1000)
            .WithMessage("Max redacted length must be between 1 and 1000 characters");

        RuleFor(x => x.CorrelationId.HeaderName)
            .NotEmpty()
            .WithMessage("Correlation ID header name cannot be empty");

        RuleFor(x => x.CorrelationId.CustomPrefix)
            .NotEmpty()
            .When(x => x.CorrelationId.Format == CorrelationIdFormat.Custom)
            .WithMessage("Custom prefix required when using custom correlation ID format");

        RuleFor(x => x.PerformanceLogging.SlowRequestThresholdMs)
            .GreaterThan(0)
            .LessThanOrEqualTo(60000) // 1 minute max
            .WithMessage("Slow request threshold must be between 1ms and 60000ms");

        RuleFor(x => x.PerformanceLogging.MetricsCollectionIntervalSeconds)
            .GreaterThan(0)
            .LessThanOrEqualTo(3600) // 1 hour max
            .WithMessage("Metrics collection interval must be between 1 and 3600 seconds");

        // Medical compliance validations
        RuleFor(x => x.EnableStructuredFormat)
            .Equal(true)
            .WithMessage("Structured format must be enabled for medical compliance");

        RuleFor(x => x.SensitiveDataRedaction.EnableRedaction)
            .Equal(true)
            .WithMessage("Sensitive data redaction must be enabled for medical compliance");

        RuleFor(x => x.CorrelationId.EnableCorrelationId)
            .Equal(true)
            .WithMessage("Correlation ID must be enabled for medical compliance audit trails");
    }
}