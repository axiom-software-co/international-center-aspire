namespace Service.Configuration.Exceptions;

/// <summary>
/// Base exception for configuration-related errors.
/// MEDICAL COMPLIANCE: Structured error handling for audit trail
/// </summary>
public abstract class ConfigurationException : Exception
{
    protected ConfigurationException(string message) : base(message)
    {
    }

    protected ConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when configuration registration fails.
/// OPTIONS PATTERN: Indicates failure during options registration process
/// </summary>
public sealed class ConfigurationRegistrationException : ConfigurationException
{
    public ConfigurationRegistrationException(string message) : base(message)
    {
    }

    public ConfigurationRegistrationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a required configuration key is not found.
/// MEDICAL COMPLIANCE: Clear error messaging for missing configuration
/// </summary>
public sealed class ConfigurationKeyNotFoundException : ConfigurationException
{
    public string ConfigurationKey { get; }

    public ConfigurationKeyNotFoundException(string message) : base(message)
    {
        ConfigurationKey = ExtractKeyFromMessage(message);
    }

    public ConfigurationKeyNotFoundException(string configurationKey, string message) : base(message)
    {
        ConfigurationKey = configurationKey;
    }

    private static string ExtractKeyFromMessage(string message)
    {
        // Simple extraction logic - in a real implementation you might want more robust parsing
        var startIndex = message.IndexOf('\'');
        var endIndex = message.LastIndexOf('\'');
        
        if (startIndex >= 0 && endIndex > startIndex)
        {
            return message.Substring(startIndex + 1, endIndex - startIndex - 1);
        }
        
        return "Unknown";
    }
}

/// <summary>
/// Exception thrown when configuration value retrieval or conversion fails.
/// TYPE SAFETY: Indicates type conversion or value processing errors
/// </summary>
public sealed class ConfigurationValueException : ConfigurationException
{
    public ConfigurationValueException(string message) : base(message)
    {
    }

    public ConfigurationValueException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when configuration validation fails.
/// MEDICAL COMPLIANCE: Ensures configuration meets medical-grade requirements
/// </summary>
public sealed class ConfigurationValidationException : ConfigurationException
{
    public IReadOnlyList<string> ValidationErrors { get; }

    public ConfigurationValidationException(string message, IReadOnlyList<string> validationErrors) : base(message)
    {
        ValidationErrors = validationErrors ?? [];
    }

    public ConfigurationValidationException(string message, IReadOnlyList<string> validationErrors, Exception innerException) 
        : base(message, innerException)
    {
        ValidationErrors = validationErrors ?? [];
    }

    public override string ToString()
    {
        var baseString = base.ToString();
        
        if (ValidationErrors.Count > 0)
        {
            var errors = string.Join(Environment.NewLine + "  - ", ValidationErrors);
            baseString += Environment.NewLine + "Validation Errors:" + Environment.NewLine + "  - " + errors;
        }
        
        return baseString;
    }
}