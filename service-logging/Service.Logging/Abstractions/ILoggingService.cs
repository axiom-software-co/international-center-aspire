using Microsoft.Extensions.Logging;

namespace Service.Logging.Abstractions;

/// <summary>
/// CONTRACT: Structured logging service interface for Services APIs with medical-grade compliance.
/// 
/// TDD PRINCIPLE: Interface drives architecture design through precondition/postcondition contracts
/// DEPENDENCY INVERSION: Abstractions for variable logging concerns (sinks, formatters, enrichers)
/// MEDICAL COMPLIANCE: Structured logging with correlation IDs, sensitive data redaction, and audit trails
/// SERVICES APIS SCOPE: Logging patterns for Services Public/Admin APIs with proper correlation
/// </summary>
public interface ILoggingService
{
    /// <summary>
    /// CONTRACT: Configure structured logging with correlation ID propagation and medical-grade templates
    /// 
    /// PRECONDITION: Valid logging configuration options and DI container
    /// POSTCONDITION: Structured logging configured with correlation ID propagation across request boundaries
    /// MEDICAL COMPLIANCE: Sensitive data redaction enabled, structured templates for audit compliance
    /// SERVICES APIS: Request/response logging templates for Public/Admin API correlation
    /// </summary>
    void ConfigureStructuredLogging();

    /// <summary>
    /// CONTRACT: Write structured log entry with correlation ID and medical-grade compliance
    /// 
    /// PRECONDITION: Valid log level, message template, and optional structured parameters
    /// POSTCONDITION: Log entry written with correlation ID, structured format, and sensitive data redacted
    /// MEDICAL COMPLIANCE: PII/PHI data automatically redacted, correlation IDs for audit trail
    /// THREAD SAFETY: Safe for concurrent logging across Services APIs request processing
    /// </summary>
    /// <param name="level">Log level (Debug, Information, Warning, Error, Critical)</param>
    /// <param name="messageTemplate">Structured message template with parameter placeholders</param>
    /// <param name="parameters">Structured parameters for message template</param>
    void WriteStructuredLog(LogLevel level, string messageTemplate, params object[] parameters);

    /// <summary>
    /// CONTRACT: Write structured log entry with exception and correlation ID
    /// 
    /// PRECONDITION: Valid exception, log level, message template, and optional parameters
    /// POSTCONDITION: Log entry with exception details, correlation ID, and structured format
    /// MEDICAL COMPLIANCE: Exception details sanitized for PII/PHI protection
    /// ERROR HANDLING: Complete exception chain logged with correlation for debugging
    /// </summary>
    /// <param name="exception">Exception to log with full details</param>
    /// <param name="level">Log level for exception (typically Warning, Error, or Critical)</param>
    /// <param name="messageTemplate">Structured message template for exception context</param>
    /// <param name="parameters">Structured parameters for additional context</param>
    void WriteStructuredLog(Exception exception, LogLevel level, string messageTemplate, params object[] parameters);

    /// <summary>
    /// CONTRACT: Begin structured logging scope with correlation ID inheritance
    /// 
    /// PRECONDITION: Valid scope name and optional structured properties
    /// POSTCONDITION: Logging scope created with correlation ID propagation and structured enrichment
    /// MEDICAL COMPLIANCE: Scope boundaries preserved for audit trail analysis
    /// SERVICES APIS: Request/response scope correlation across Public/Admin API boundaries
    /// RESOURCE MANAGEMENT: IDisposable scope for proper cleanup
    /// </summary>
    /// <param name="scopeName">Name identifier for logging scope</param>
    /// <param name="properties">Optional structured properties to enrich scope</param>
    /// <returns>Disposable logging scope with correlation ID propagation</returns>
    IDisposable BeginStructuredScope(string scopeName, params (string Key, object Value)[] properties);

    /// <summary>
    /// CONTRACT: Get current correlation ID for request tracing across Services APIs
    /// 
    /// POSTCONDITION: Returns current correlation ID or generates new one if none exists
    /// MEDICAL COMPLIANCE: Correlation ID preserved for complete audit trail
    /// SERVICES APIS: Request correlation across Public Gateway -> Services APIs -> databases
    /// THREAD SAFETY: Correlation ID isolated per logical request context
    /// </summary>
    /// <returns>Current correlation ID for request tracing</returns>
    string GetCurrentCorrelationId();

    /// <summary>
    /// CONTRACT: Set correlation ID for request tracing in Services APIs
    /// 
    /// PRECONDITION: Valid correlation ID string
    /// POSTCONDITION: Correlation ID set for current logical context and propagated to child scopes
    /// MEDICAL COMPLIANCE: Correlation ID validation for medical-grade audit requirements
    /// SERVICES APIS: Gateway correlation ID propagated to Services Public/Admin APIs
    /// THREAD SAFETY: Correlation ID scoped to logical request context
    /// </summary>
    /// <param name="correlationId">Correlation ID for request tracing</param>
    void SetCorrelationId(string correlationId);

    /// <summary>
    /// CONTRACT: Validate structured logging configuration for medical-grade compliance
    /// 
    /// POSTCONDITION: Returns validation result indicating compliance with medical logging requirements
    /// MEDICAL COMPLIANCE: Validates structured templates, retention policies, sensitive data redaction
    /// SERVICES APIS: Confirms logging patterns meet Services Public/Admin APIs audit requirements
    /// STARTUP VALIDATION: Called during application startup to ensure logging compliance
    /// </summary>
    /// <returns>Validation result with medical compliance status and any configuration issues</returns>
    LoggingValidationResult ValidateLoggingConfiguration();

    /// <summary>
    /// CONTRACT: Get logging configuration context for medical-grade audit purposes
    /// 
    /// POSTCONDITION: Returns current logging environment with medical compliance settings
    /// MEDICAL COMPLIANCE: Logging context includes retention policies, redaction status, audit configuration
    /// SERVICES APIS: Environment context for Services Public/Admin APIs logging configuration
    /// AUDIT TRAIL: Configuration context logged for compliance verification
    /// </summary>
    /// <returns>Current logging environment configuration context</returns>
    LoggingEnvironmentContext GetLoggingEnvironmentContext();
}

/// <summary>
/// Structured logging scope with correlation ID propagation and medical compliance.
/// MEDICAL COMPLIANCE: Scope boundaries preserved for audit trail analysis
/// </summary>
public interface IStructuredLoggingScope : IDisposable
{
    /// <summary>Correlation ID for this logging scope</summary>
    string CorrelationId { get; }
    
    /// <summary>Scope name for structured logging</summary>
    string ScopeName { get; }
    
    /// <summary>Structured properties enriching this scope</summary>
    IReadOnlyDictionary<string, object> Properties { get; }
    
    /// <summary>Timestamp when scope began</summary>
    DateTime StartedAt { get; }
}

/// <summary>
/// Logging configuration validation result for medical-grade compliance.
/// MEDICAL COMPLIANCE: Validation ensures logging meets medical standards
/// </summary>
public sealed class LoggingValidationResult
{
    /// <summary>Indicates if logging configuration is valid for medical compliance</summary>
    public required bool IsValid { get; init; }
    
    /// <summary>Validation errors preventing medical compliance</summary>
    public required IReadOnlyList<string> Errors { get; init; }
    
    /// <summary>Validation warnings for logging configuration</summary>
    public required IReadOnlyList<string> Warnings { get; init; }
    
    /// <summary>Timestamp when validation was performed</summary>
    public required DateTime ValidatedAt { get; init; }
    
    /// <summary>Medical compliance features validated</summary>
    public required IReadOnlyList<string> ValidatedFeatures { get; init; }
}

/// <summary>
/// Logging environment context for medical-grade audit and configuration verification.
/// MEDICAL COMPLIANCE: Environment context provides audit trail for logging configuration
/// </summary>
public sealed class LoggingEnvironmentContext
{
    /// <summary>Current environment (Development, Testing, Production)</summary>
    public required string Environment { get; init; }
    
    /// <summary>Structured logging enabled status</summary>
    public required bool StructuredLoggingEnabled { get; init; }
    
    /// <summary>Correlation ID propagation enabled status</summary>
    public required bool CorrelationIdEnabled { get; init; }
    
    /// <summary>Sensitive data redaction enabled status</summary>
    public required bool SensitiveDataRedactionEnabled { get; init; }
    
    /// <summary>Medical compliance logging enabled status</summary>
    public required bool MedicalComplianceLoggingEnabled { get; init; }
    
    /// <summary>Log retention period in days for medical compliance</summary>
    public required int LogRetentionDays { get; init; }
    
    /// <summary>Configured logging sinks (Console, File, Seq, etc.)</summary>
    public required IReadOnlyList<string> ConfiguredSinks { get; init; }
    
    /// <summary>Minimum logging level configured</summary>
    public required string MinimumLevel { get; init; }
    
    /// <summary>Logging configuration properties for audit</summary>
    public required IReadOnlyDictionary<string, string> ConfigurationProperties { get; init; }
}