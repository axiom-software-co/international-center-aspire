using FluentValidation;

namespace Service.Configuration.Options;

/// <summary>
/// Logging configuration options for structured logging across Services APIs.
/// MEDICAL COMPLIANCE: Ensures proper audit trail and log retention
/// STRUCTURED LOGGING: No string concatenation, correlation IDs required
/// </summary>
public sealed class LoggingOptions
{
    public const string SectionName = "Logging";

    /// <summary>
    /// Minimum log level for Services APIs
    /// MEDICAL COMPLIANCE: Debug level required for compliance audit
    /// </summary>
    public LogLevel MinimumLevel { get; init; } = LogLevel.Information;

    /// <summary>
    /// Enable correlation ID generation and propagation
    /// MEDICAL COMPLIANCE: Required for tracing requests across Services APIs
    /// </summary>
    public bool EnableCorrelationIds { get; init; } = true;

    /// <summary>
    /// Enable structured logging with key-value pairs
    /// MEDICAL COMPLIANCE: Structured data required for audit compliance
    /// </summary>
    public bool EnableStructuredLogging { get; init; } = true;

    /// <summary>
    /// Include user ID in all log entries (when available)
    /// MEDICAL COMPLIANCE: User attribution required for audit trail
    /// </summary>
    public bool IncludeUserIdInLogs { get; init; } = true;

    /// <summary>
    /// Include request URL in all log entries
    /// MEDICAL COMPLIANCE: Full request context required for audit
    /// </summary>
    public bool IncludeRequestUrlInLogs { get; init; } = true;

    /// <summary>
    /// Include application version in all log entries
    /// MEDICAL COMPLIANCE: Version tracking required for compliance
    /// </summary>
    public bool IncludeAppVersionInLogs { get; init; } = true;

    /// <summary>
    /// Medical-grade log retention period in days
    /// MEDICAL COMPLIANCE: Extended retention required for medical data
    /// </summary>
    public int LogRetentionDays { get; init; } = 2555; // ~7 years for medical compliance

    /// <summary>
    /// Enable sensitive data redaction in logs
    /// MEDICAL COMPLIANCE: PII and PHI must be redacted from logs
    /// </summary>
    public bool EnableSensitiveDataRedaction { get; init; } = true;

    /// <summary>
    /// List of properties to redact from log output
    /// MEDICAL COMPLIANCE: Common sensitive fields automatically redacted
    /// </summary>
    public IReadOnlyList<string> SensitivePropertyNames { get; init; } = 
    [
        "Password", "Secret", "Token", "Key", "ConnectionString",
        "PatientId", "MedicalRecordNumber", "SSN", "DateOfBirth"
    ];

    /// <summary>
    /// Log file configuration for persistent logging
    /// MEDICAL COMPLIANCE: File-based logging required for audit trail
    /// </summary>
    public LogFileOptions FileLogging { get; init; } = new();

    /// <summary>
    /// Console logging configuration for development
    /// </summary>
    public ConsoleLoggingOptions ConsoleLogging { get; init; } = new();

    /// <summary>
    /// Performance logging configuration for monitoring
    /// </summary>
    public PerformanceLoggingOptions PerformanceLogging { get; init; } = new();
}

/// <summary>
/// Log file configuration for persistent medical-grade logging.
/// </summary>
public sealed class LogFileOptions
{
    /// <summary>Base path for log files</summary>
    public string BasePath { get; init; } = "./logs";

    /// <summary>Log file name pattern with date</summary>
    public string FileNamePattern { get; init; } = "services-{Date}.log";

    /// <summary>Maximum file size before rolling in MB</summary>
    public int MaxFileSizeMB { get; init; } = 100;

    /// <summary>Maximum number of log files to retain</summary>
    public int MaxFileCount { get; init; } = 365; // ~1 year of daily logs

    /// <summary>Enable JSON formatted log files for structured parsing</summary>
    public bool UseJsonFormat { get; init; } = true;
}

/// <summary>
/// Console logging configuration for development environments.
/// </summary>
public sealed class ConsoleLoggingOptions
{
    /// <summary>Enable console logging output</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Enable colored console output for readability</summary>
    public bool EnableColors { get; init; } = true;

    /// <summary>Console-specific minimum log level</summary>
    public LogLevel MinimumLevel { get; init; } = LogLevel.Information;
}

/// <summary>
/// Performance logging configuration for monitoring Services APIs.
/// </summary>
public sealed class PerformanceLoggingOptions
{
    /// <summary>Enable automatic performance logging for API endpoints</summary>
    public bool EnableEndpointTiming { get; init; } = true;

    /// <summary>Enable database operation performance logging</summary>
    public bool EnableDatabaseTiming { get; init; } = true;

    /// <summary>Enable Redis operation performance logging</summary>
    public bool EnableCacheTiming { get; init; } = true;

    /// <summary>Threshold in milliseconds for slow operation warnings</summary>
    public int SlowOperationThresholdMs { get; init; } = 1000;
}

/// <summary>
/// Log levels matching Microsoft.Extensions.Logging.LogLevel
/// </summary>
public enum LogLevel
{
    Trace = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Critical = 5,
    None = 6
}

/// <summary>
/// FluentValidation validator for LoggingOptions.
/// MEDICAL COMPLIANCE: Ensures logging configuration meets medical standards
/// </summary>
public sealed class LoggingOptionsValidator : AbstractValidator<LoggingOptions>
{
    public LoggingOptionsValidator()
    {
        RuleFor(x => x.MinimumLevel)
            .IsInEnum()
            .WithMessage("Invalid minimum log level specified");

        RuleFor(x => x.LogRetentionDays)
            .GreaterThan(0)
            .LessThanOrEqualTo(3650) // 10 years max
            .WithMessage("Log retention days must be between 1 and 3650 days");

        RuleFor(x => x.SensitivePropertyNames)
            .NotNull()
            .WithMessage("Sensitive property names list cannot be null");

        RuleFor(x => x.FileLogging.MaxFileSizeMB)
            .GreaterThan(0)
            .LessThanOrEqualTo(1000)
            .WithMessage("Max file size must be between 1 and 1000 MB");

        RuleFor(x => x.FileLogging.MaxFileCount)
            .GreaterThan(0)
            .LessThanOrEqualTo(3650)
            .WithMessage("Max file count must be between 1 and 3650 files");

        RuleFor(x => x.FileLogging.BasePath)
            .NotEmpty()
            .WithMessage("Log file base path is required");

        RuleFor(x => x.PerformanceLogging.SlowOperationThresholdMs)
            .GreaterThan(0)
            .LessThanOrEqualTo(60000) // 1 minute max
            .WithMessage("Slow operation threshold must be between 1 and 60000 milliseconds");

        // Medical compliance validation
        RuleFor(x => x)
            .Must(x => x.EnableStructuredLogging)
            .WithMessage("Structured logging must be enabled for medical compliance")
            .Must(x => x.EnableCorrelationIds)
            .WithMessage("Correlation IDs must be enabled for medical compliance")
            .Must(x => x.EnableSensitiveDataRedaction)
            .WithMessage("Sensitive data redaction must be enabled for medical compliance")
            .Must(x => x.LogRetentionDays >= 2555) // ~7 years minimum
            .WithMessage("Log retention period must be at least 7 years for medical compliance");
    }
}