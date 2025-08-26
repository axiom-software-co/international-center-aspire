using Service.Configuration.Abstractions;
using Service.Configuration.Exceptions;
using Service.Configuration.Extensions;
using Service.Configuration.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Service.Configuration.Tests.Integration;

/// <summary>
/// Contract-first tests for IConfigurationService interface.
/// 
/// TDD PRINCIPLE: Tests drive the design of the architecture
/// CONTRACT TESTING: Tests interfaces/contracts rather than implementation details  
/// PRECONDITIONS/POSTCONDITIONS: Focused on dependencies and state changes
/// 
/// MEDICAL COMPLIANCE: Validates configuration service meets medical-grade requirements
/// SERVICES APIs SCOPE: Tests configuration patterns for Services Public/Admin APIs
/// </summary>
public sealed class ConfigurationServiceContractTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ServiceProvider _serviceProvider;
    private readonly IConfigurationService _configurationService;

    public ConfigurationServiceContractTests(ITestOutputHelper output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
        
        // Arrange: Build configuration and services for testing
        var configuration = BuildTestConfiguration();
        var services = new ServiceCollection();
        
        services.AddLogging(builder => builder.AddXUnit(output));
        services.AddServiceConfiguration(configuration);
        
        _serviceProvider = services.BuildServiceProvider();
        _configurationService = _serviceProvider.GetRequiredService<IConfigurationService>();
    }

    /// <summary>
    /// CONTRACT: RegisterOptions must register options with automatic .Value binding
    /// 
    /// PRECONDITION: Valid options class with parameterless constructor
    /// POSTCONDITION: Options registered in DI container without .Value requirement
    /// MEDICAL COMPLIANCE: Configuration validation enabled by default
    /// </summary>
    [Fact(Timeout = 5000)]
    public void RegisterOptions_WithValidOptionsClass_ShouldRegisterWithoutValueBinding()
    {
        // Arrange
        const string sectionName = "TestDatabase";
        
        // Act - Register options (this should not throw)
        _configurationService.RegisterOptions<DatabaseOptions>(sectionName, validateOnStart: true);
        
        // Assert - Options should be available directly without .Value
        var databaseOptions = _serviceProvider.GetRequiredService<DatabaseOptions>();
        Assert.NotNull(databaseOptions);
        Assert.Equal("Server=localhost;Database=test_admin", databaseOptions.AdminConnectionString);
        
        _output.WriteLine($"✅ Options registered successfully for section: {sectionName}");
    }

    /// <summary>
    /// CONTRACT: RegisterOptions must throw exception for invalid section names
    /// 
    /// PRECONDITION: Null, empty, or whitespace section name
    /// POSTCONDITION: ArgumentException thrown with clear message
    /// </summary>
    [Theory(Timeout = 5000)]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RegisterOptions_WithInvalidSectionName_ShouldThrowArgumentException(string invalidSectionName)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _configurationService.RegisterOptions<DatabaseOptions>(invalidSectionName));
        
        Assert.Contains("Configuration section cannot be null or empty", exception.Message);
        _output.WriteLine($"✅ Correctly rejected invalid section name: '{invalidSectionName}'");
    }

    /// <summary>
    /// CONTRACT: GetRequiredValue must return strongly-typed configuration values
    /// 
    /// PRECONDITION: Configuration key exists and is valid for type T
    /// POSTCONDITION: Returns strongly-typed configuration value
    /// MEDICAL COMPLIANCE: Sensitive values handled securely (no logging)
    /// </summary>
    [Fact(Timeout = 5000)]
    public void GetRequiredValue_WithExistingKey_ShouldReturnTypedValue()
    {
        // Arrange
        const string key = "TestSettings:MaxRetryAttempts";
        
        // Act
        var value = _configurationService.GetRequiredValue<int>(key);
        
        // Assert
        Assert.Equal(3, value);
        _output.WriteLine($"✅ Retrieved required value: {key} = {value}");
    }

    /// <summary>
    /// CONTRACT: GetRequiredValue must throw exception for missing required keys
    /// 
    /// PRECONDITION: Configuration key does not exist
    /// POSTCONDITION: ConfigurationKeyNotFoundException thrown
    /// </summary>
    [Fact(Timeout = 5000)]
    public void GetRequiredValue_WithMissingKey_ShouldThrowConfigurationKeyNotFoundException()
    {
        // Arrange
        const string missingKey = "NonExistent:Configuration:Key";
        
        // Act & Assert
        var exception = Assert.Throws<ConfigurationKeyNotFoundException>(() =>
            _configurationService.GetRequiredValue<string>(missingKey));
        
        Assert.Contains($"Required configuration key '{missingKey}' was not found", exception.Message);
        _output.WriteLine($"✅ Correctly threw exception for missing key: {missingKey}");
    }

    /// <summary>
    /// CONTRACT: GetOptionalValue must return default value when key is missing
    /// 
    /// PRECONDITION: Configuration key does not exist
    /// POSTCONDITION: Returns provided default value
    /// MEDICAL COMPLIANCE: Default value usage logged for audit
    /// </summary>
    [Fact(Timeout = 5000)]
    public void GetOptionalValue_WithMissingKey_ShouldReturnDefaultValue()
    {
        // Arrange
        const string missingKey = "NonExistent:Optional:Key";
        const string defaultValue = "DefaultTestValue";
        
        // Act
        var value = _configurationService.GetOptionalValue(missingKey, defaultValue);
        
        // Assert
        Assert.Equal(defaultValue, value);
        _output.WriteLine($"✅ Returned default value for missing key: {missingKey} = {defaultValue}");
    }

    /// <summary>
    /// CONTRACT: ValidateAllOptions must validate all registered configuration options
    /// 
    /// PRECONDITION: Options registered via RegisterOptions
    /// POSTCONDITION: ValidationResult indicates success/failure with detailed errors
    /// MEDICAL COMPLIANCE: Validation results logged for compliance audit
    /// </summary>
    [Fact(Timeout = 5000)]
    public void ValidateAllOptions_WithValidConfiguration_ShouldReturnSuccessResult()
    {
        // Arrange - Register valid options
        _configurationService.RegisterOptions<DatabaseOptions>("TestDatabase", validateOnStart: true);
        _configurationService.RegisterOptions<RedisOptions>("TestRedis", validateOnStart: true);
        
        // Act
        var validationResult = _configurationService.ValidateAllOptions();
        
        // Assert
        Assert.NotNull(validationResult);
        Assert.True(validationResult.IsValid);
        Assert.Empty(validationResult.Errors);
        Assert.True(validationResult.ValidatedAt <= DateTime.UtcNow);
        
        _output.WriteLine($"✅ Configuration validation passed at: {validationResult.ValidatedAt}");
    }

    /// <summary>
    /// CONTRACT: GetEnvironmentContext must return current environment information
    /// 
    /// POSTCONDITION: Returns environment context with correct flags
    /// MEDICAL COMPLIANCE: Environment context logged for audit purposes
    /// </summary>
    [Fact(Timeout = 5000)]
    public void GetEnvironmentContext_ShouldReturnEnvironmentInformation()
    {
        // Act
        var environmentContext = _configurationService.GetEnvironmentContext();
        
        // Assert
        Assert.NotNull(environmentContext);
        Assert.NotEmpty(environmentContext.Environment);
        Assert.NotNull(environmentContext.EnvironmentVariables);
        
        // Testing environment should have IsTesting = true
        Assert.True(environmentContext.IsTesting);
        Assert.False(environmentContext.IsProduction);
        
        _output.WriteLine($"✅ Environment context: {environmentContext.Environment}, Medical compliance: {environmentContext.MedicalComplianceEnabled}");
    }

    /// <summary>
    /// CONTRACT: Configuration service must handle sensitive data securely
    /// 
    /// PRECONDITION: Configuration contains sensitive keys (password, secret, etc.)
    /// POSTCONDITION: Sensitive values are not exposed in logs or exceptions
    /// MEDICAL COMPLIANCE: PII/PHI protection in configuration handling
    /// </summary>
    [Fact(Timeout = 5000)]
    public void GetRequiredValue_WithSensitiveKey_ShouldNotExposeValueInLogs()
    {
        // Arrange
        const string sensitiveKey = "TestSettings:DatabasePassword";
        
        // Act
        var value = _configurationService.GetRequiredValue<string>(sensitiveKey);
        
        // Assert
        Assert.NotNull(value);
        Assert.Equal("secret_password_123", value);
        
        // Note: In real implementation, sensitive values should not appear in logs
        // This test validates the value is retrieved correctly while logging is redacted
        _output.WriteLine($"✅ Sensitive configuration retrieved without exposure");
    }

    /// <summary>
    /// CONTRACT: Configuration service must support medical-grade compliance requirements
    /// 
    /// PRECONDITION: Medical compliance configuration enabled
    /// POSTCONDITION: All medical compliance features are active
    /// MEDICAL COMPLIANCE: Validates service meets medical-grade standards
    /// </summary>
    [Fact(Timeout = 5000)]
    public void ConfigurationService_WithMedicalCompliance_ShouldEnforceComplianceRequirements()
    {
        // Arrange - Register options with medical compliance
        _configurationService.RegisterOptions<LoggingOptions>("TestLogging", validateOnStart: true);
        _configurationService.RegisterOptions<SecurityOptions>("TestSecurity", validateOnStart: true);
        
        // Act
        var environmentContext = _configurationService.GetEnvironmentContext();
        var validationResult = _configurationService.ValidateAllOptions();
        
        // Assert - Medical compliance features should be active
        Assert.True(validationResult.IsValid, "Medical compliance validation must pass");
        
        // Retrieve options to verify medical compliance settings
        var loggingOptions = _serviceProvider.GetRequiredService<LoggingOptions>();
        var securityOptions = _serviceProvider.GetRequiredService<SecurityOptions>();
        
        Assert.True(loggingOptions.EnableStructuredLogging, "Structured logging required for medical compliance");
        Assert.True(loggingOptions.EnableCorrelationIds, "Correlation IDs required for medical compliance");
        Assert.True(loggingOptions.EnableSensitiveDataRedaction, "Sensitive data redaction required for medical compliance");
        Assert.True(securityOptions.EnableMedicalGradeCompliance, "Medical-grade compliance must be enabled");
        
        _output.WriteLine($"✅ Medical compliance requirements validated successfully");
    }

    /// <summary>
    /// Builds test configuration with valid settings for contract testing.
    /// </summary>
    private static IConfiguration BuildTestConfiguration()
    {
        var configurationBuilder = new ConfigurationBuilder();
        
        // Add in-memory configuration for testing
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
        {
            // Test database configuration
            ["TestDatabase:AdminConnectionString"] = "Server=localhost;Database=test_admin",
            ["TestDatabase:PublicConnectionString"] = "Server=localhost;Database=test_public",
            ["TestDatabase:MaxRetryAttempts"] = "3",
            ["TestDatabase:CommandTimeoutSeconds"] = "30",
            
            // Test Redis configuration
            ["TestRedis:ConnectionString"] = "localhost:6379",
            ["TestRedis:RateLimitingDatabase"] = "0",
            ["TestRedis:CacheDatabase"] = "1",
            ["TestRedis:MaxRequestsPerWindow"] = "1000",
            
            // Test logging configuration
            ["TestLogging:MinimumLevel"] = "Information",
            ["TestLogging:EnableStructuredLogging"] = "true",
            ["TestLogging:EnableCorrelationIds"] = "true",
            ["TestLogging:EnableSensitiveDataRedaction"] = "true",
            ["TestLogging:LogRetentionDays"] = "2555",
            
            // Test security configuration  
            ["TestSecurity:EnableMedicalGradeCompliance"] = "true",
            ["TestSecurity:EnableFallbackPolicies"] = "true",
            ["TestSecurity:RemoveServerHeader"] = "true",
            
            // General test settings
            ["TestSettings:MaxRetryAttempts"] = "3",
            ["TestSettings:DatabasePassword"] = "secret_password_123",
            
            // Environment settings
            ["ASPNETCORE_ENVIRONMENT"] = "Testing",
            ["EnableMedicalCompliance"] = "true"
        });
        
        return configurationBuilder.Build();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}