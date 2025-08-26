namespace Service.Configuration.Abstractions;

/// <summary>
/// Contract for configuration service that manages Options pattern registration
/// and environment-specific configuration management for Services APIs.
/// 
/// DEPENDENCY INVERSION: Interface for variable concerns (configuration sources may change)
/// MEDICAL COMPLIANCE: Ensures secure configuration handling and validation
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Registers configuration options with automatic .Value binding to eliminate .Value calls.
    /// 
    /// PRECONDITION: TOptions must be a valid options class with parameterless constructor
    /// POSTCONDITION: Options are registered in DI container with .Value binding
    /// MEDICAL COMPLIANCE: Configuration validation occurs during registration
    /// </summary>
    /// <typeparam name="TOptions">Options class to register</typeparam>
    /// <param name="configurationSection">Configuration section key</param>
    /// <param name="validateOnStart">Whether to validate configuration on application start</param>
    void RegisterOptions<TOptions>(string configurationSection, bool validateOnStart = true) 
        where TOptions : class, new();

    /// <summary>
    /// Gets configuration value with type safety and validation.
    /// 
    /// PRECONDITION: Configuration key must exist and be valid for type T
    /// POSTCONDITION: Returns strongly-typed configuration value or throws ConfigurationException
    /// MEDICAL COMPLIANCE: Sensitive values are handled securely (no logging of secrets)
    /// </summary>
    /// <typeparam name="T">Type to bind configuration to</typeparam>
    /// <param name="key">Configuration key</param>
    /// <returns>Strongly-typed configuration value</returns>
    T GetRequiredValue<T>(string key);

    /// <summary>
    /// Gets optional configuration value with default fallback.
    /// 
    /// PRECONDITION: Type T must be nullable or have parameterless constructor
    /// POSTCONDITION: Returns configuration value or provided default
    /// MEDICAL COMPLIANCE: Default values logged for audit purposes
    /// </summary>
    /// <typeparam name="T">Type to bind configuration to</typeparam>
    /// <param name="key">Configuration key</param>
    /// <param name="defaultValue">Default value if key not found</param>
    /// <returns>Configuration value or default</returns>
    T? GetOptionalValue<T>(string key, T? defaultValue = default);

    /// <summary>
    /// Validates all registered configuration options.
    /// 
    /// PRECONDITION: All options must have been registered via RegisterOptions
    /// POSTCONDITION: All configuration is valid or ConfigurationException is thrown
    /// MEDICAL COMPLIANCE: Validation results are logged for compliance audit
    /// </summary>
    /// <returns>Configuration validation result with any errors</returns>
    ConfigurationValidationResult ValidateAllOptions();

    /// <summary>
    /// Gets current environment configuration context.
    /// 
    /// POSTCONDITION: Returns current environment (Development/Testing/Production)
    /// MEDICAL COMPLIANCE: Environment context logged for audit purposes
    /// </summary>
    /// <returns>Current environment configuration</returns>
    EnvironmentContext GetEnvironmentContext();
}

/// <summary>
/// Configuration validation result for medical-grade compliance reporting.
/// </summary>
public sealed record ConfigurationValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public DateTime ValidatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Environment context for environment-specific configuration behavior.
/// </summary>
public sealed record EnvironmentContext
{
    public required string Environment { get; init; }
    public required bool IsDevelopment { get; init; }
    public required bool IsTesting { get; init; }
    public required bool IsProduction { get; init; }
    public required bool MedicalComplianceEnabled { get; init; }
    public required IDictionary<string, string> EnvironmentVariables { get; init; }
}