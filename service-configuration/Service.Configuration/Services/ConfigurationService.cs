using FluentValidation;
using Service.Configuration.Abstractions;
using Service.Configuration.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Service.Configuration.Services;

/// <summary>
/// Configuration service implementation providing Options pattern with automatic .Value registration.
/// DEPENDENCY INVERSION: Concrete implementation of IConfigurationService interface
/// MEDICAL COMPLIANCE: Secure configuration handling with validation and audit
/// </summary>
public sealed class ConfigurationService : IConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceCollection _services;
    private readonly ILogger<ConfigurationService> _logger;
    private readonly Dictionary<Type, string> _registeredOptions = new();

    public ConfigurationService(
        IConfiguration configuration,
        IServiceCollection services,
        ILogger<ConfigurationService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers configuration options with automatic .Value binding to eliminate .Value calls.
    /// 
    /// IMPLEMENTATION: Uses Microsoft.Extensions.Options pattern with validation
    /// MEDICAL COMPLIANCE: Configuration validation occurs during registration
    /// </summary>
    public void RegisterOptions<TOptions>(string configurationSection, bool validateOnStart = true) 
        where TOptions : class, new()
    {
        if (string.IsNullOrWhiteSpace(configurationSection))
        {
            throw new ArgumentException("Configuration section cannot be null or empty", nameof(configurationSection));
        }

        var optionsType = typeof(TOptions);
        
        _logger.LogInformation("Registering options {OptionsType} from section {Section} with validation: {ValidateOnStart}",
            optionsType.Name, configurationSection, validateOnStart);

        try
        {
            // Register configuration options with binding
            var optionsBuilder = _services.Configure<TOptions>(_configuration.GetSection(configurationSection));

            // Add validation if enabled
            if (validateOnStart)
            {
                // Look for FluentValidation validator
                var validatorType = FindValidatorType<TOptions>();
                if (validatorType != null)
                {
                    _services.AddScoped(validatorType);
                    optionsBuilder.Services.AddSingleton<IValidateOptions<TOptions>>(provider =>
                        new FluentValidationOptionsValidator<TOptions>(
                            provider.GetRequiredService(validatorType) as IValidator<TOptions>));
                }
                
                // Enable validation on startup
                _services.AddOptions<TOptions>()
                    .ValidateOnStart();
            }

            // Register direct access without .Value (eliminating .Value calls everywhere)
            _services.AddSingleton<TOptions>(provider =>
                provider.GetRequiredService<IOptions<TOptions>>().Value);

            // Track registered options for validation
            _registeredOptions[optionsType] = configurationSection;

            _logger.LogInformation("Successfully registered options {OptionsType} from section {Section}",
                optionsType.Name, configurationSection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register options {OptionsType} from section {Section}",
                optionsType.Name, configurationSection);
            throw new ConfigurationRegistrationException(
                $"Failed to register options {optionsType.Name} from section {configurationSection}", ex);
        }
    }

    /// <summary>
    /// Gets configuration value with type safety and validation.
    /// 
    /// IMPLEMENTATION: Uses IConfiguration.GetValue with error handling
    /// MEDICAL COMPLIANCE: Sensitive values are handled securely (no logging of secrets)
    /// </summary>
    public T GetRequiredValue<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Configuration key cannot be null or empty", nameof(key));
        }

        try
        {
            var value = _configuration.GetValue<T>(key);
            
            if (value == null)
            {
                throw new ConfigurationKeyNotFoundException($"Required configuration key '{key}' was not found");
            }

            // Log access without exposing sensitive values
            var logValue = IsSensitiveKey(key) ? "[REDACTED]" : value.ToString();
            _logger.LogDebug("Retrieved required configuration value for key {Key}: {Value}", key, logValue);

            return value;
        }
        catch (Exception ex) when (!(ex is ConfigurationKeyNotFoundException))
        {
            _logger.LogError(ex, "Failed to retrieve required configuration value for key {Key}", key);
            throw new ConfigurationValueException($"Failed to retrieve configuration value for key '{key}'", ex);
        }
    }

    /// <summary>
    /// Gets optional configuration value with default fallback.
    /// 
    /// IMPLEMENTATION: Uses IConfiguration.GetValue with default handling
    /// MEDICAL COMPLIANCE: Default values logged for audit purposes
    /// </summary>
    public T? GetOptionalValue<T>(string key, T? defaultValue = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Configuration key cannot be null or empty", nameof(key));
        }

        try
        {
            var value = _configuration.GetValue<T>(key);
            
            if (value == null)
            {
                _logger.LogDebug("Configuration key {Key} not found, using default value: {DefaultValue}", 
                    key, defaultValue);
                return defaultValue;
            }

            // Log access without exposing sensitive values
            var logValue = IsSensitiveKey(key) ? "[REDACTED]" : value.ToString();
            _logger.LogDebug("Retrieved optional configuration value for key {Key}: {Value}", key, logValue);

            return value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve optional configuration value for key {Key}, using default: {DefaultValue}",
                key, defaultValue);
            return defaultValue;
        }
    }

    /// <summary>
    /// Validates all registered configuration options.
    /// 
    /// IMPLEMENTATION: Uses IValidateOptions<T> for each registered option
    /// MEDICAL COMPLIANCE: Validation results are logged for compliance audit
    /// </summary>
    public ConfigurationValidationResult ValidateAllOptions()
    {
        _logger.LogInformation("Validating {OptionCount} registered configuration options", _registeredOptions.Count);

        var errors = new List<string>();
        var warnings = new List<string>();

        foreach (var (optionType, section) in _registeredOptions)
        {
            try
            {
                // Use reflection to call ValidateOptions for each registered type
                var method = typeof(ConfigurationService).GetMethod(nameof(ValidateOptions), 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var genericMethod = method?.MakeGenericMethod(optionType);
                
                var result = genericMethod?.Invoke(this, [section]) as (bool IsValid, List<string> Errors);
                
                if (result?.IsValid == false)
                {
                    errors.AddRange(result.Value.Errors.Select(e => $"{optionType.Name}: {e}"));
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Failed to validate options {optionType.Name}: {ex.Message}";
                _logger.LogError(ex, "Configuration validation failed for {OptionsType}", optionType.Name);
                errors.Add(errorMessage);
            }
        }

        var isValid = errors.Count == 0;
        var result = new ConfigurationValidationResult
        {
            IsValid = isValid,
            Errors = errors.AsReadOnly(),
            Warnings = warnings.AsReadOnly(),
            ValidatedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Configuration validation completed: {IsValid}. Errors: {ErrorCount}, Warnings: {WarningCount}",
            isValid, errors.Count, warnings.Count);

        if (!isValid)
        {
            _logger.LogError("Configuration validation failed with {ErrorCount} errors: {Errors}",
                errors.Count, string.Join("; ", errors));
        }

        return result;
    }

    /// <summary>
    /// Gets current environment configuration context.
    /// 
    /// IMPLEMENTATION: Reads from ASPNETCORE_ENVIRONMENT and other standard variables
    /// MEDICAL COMPLIANCE: Environment context logged for audit purposes
    /// </summary>
    public EnvironmentContext GetEnvironmentContext()
    {
        var environment = GetOptionalValue("ASPNETCORE_ENVIRONMENT", "Development") ?? "Development";
        var isDevelopment = environment.Equals("Development", StringComparison.OrdinalIgnoreCase);
        var isTesting = environment.Equals("Testing", StringComparison.OrdinalIgnoreCase);
        var isProduction = environment.Equals("Production", StringComparison.OrdinalIgnoreCase);

        // Medical compliance enabled by default in Production, optional in other environments
        var medicalComplianceEnabled = isProduction || 
                                      GetOptionalValue<bool>("EnableMedicalCompliance", isProduction);

        var environmentVariables = new Dictionary<string, string>
        {
            ["ASPNETCORE_ENVIRONMENT"] = environment,
            ["DOTNET_ENVIRONMENT"] = GetOptionalValue("DOTNET_ENVIRONMENT", environment) ?? environment,
            ["EnableMedicalCompliance"] = medicalComplianceEnabled.ToString()
        };

        var context = new EnvironmentContext
        {
            Environment = environment,
            IsDevelopment = isDevelopment,
            IsTesting = isTesting,
            IsProduction = isProduction,
            MedicalComplianceEnabled = medicalComplianceEnabled,
            EnvironmentVariables = environmentVariables
        };

        _logger.LogInformation("Environment context: {Environment}, Medical compliance: {MedicalCompliance}",
            environment, medicalComplianceEnabled);

        return context;
    }

    /// <summary>
    /// Validates specific options type using FluentValidation.
    /// </summary>
    private (bool IsValid, List<string> Errors) ValidateOptions<T>(string section) where T : class
    {
        try
        {
            var optionsSection = _configuration.GetSection(section);
            var options = optionsSection.Get<T>();
            
            if (options == null)
            {
                return (false, [$"Configuration section '{section}' not found or could not be bound to type {typeof(T).Name}"]);
            }

            var validatorType = FindValidatorType<T>();
            if (validatorType == null)
            {
                return (true, []); // No validator found, assume valid
            }

            var validator = Activator.CreateInstance(validatorType) as IValidator<T>;
            if (validator == null)
            {
                return (true, []); // Could not create validator, assume valid
            }

            var validationResult = validator.Validate(options);
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();

            return (validationResult.IsValid, errors);
        }
        catch (Exception ex)
        {
            return (false, [$"Validation failed: {ex.Message}"]);
        }
    }

    /// <summary>
    /// Finds FluentValidation validator type for the given options type.
    /// </summary>
    private static Type? FindValidatorType<T>()
    {
        var optionsType = typeof(T);
        var validatorTypeName = $"{optionsType.Name}Validator";
        
        return optionsType.Assembly
            .GetTypes()
            .FirstOrDefault(t => t.Name == validatorTypeName && 
                                typeof(IValidator<T>).IsAssignableFrom(t));
    }

    /// <summary>
    /// Checks if a configuration key contains sensitive information.
    /// MEDICAL COMPLIANCE: Prevents logging of sensitive configuration values
    /// </summary>
    private static bool IsSensitiveKey(string key)
    {
        var sensitiveKeywords = new[]
        {
            "password", "secret", "key", "token", "connectionstring",
            "private", "credential", "auth", "signature"
        };

        return sensitiveKeywords.Any(keyword => 
            key.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// FluentValidation options validator for integrating with IValidateOptions.
/// </summary>
internal sealed class FluentValidationOptionsValidator<T> : IValidateOptions<T> where T : class
{
    private readonly IValidator<T> _validator;

    public FluentValidationOptionsValidator(IValidator<T> validator)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public ValidateOptionsResult Validate(string? name, T options)
    {
        var result = _validator.Validate(options);
        
        if (result.IsValid)
        {
            return ValidateOptionsResult.Success;
        }

        var errors = result.Errors.Select(e => e.ErrorMessage);
        return ValidateOptionsResult.Fail(errors);
    }
}