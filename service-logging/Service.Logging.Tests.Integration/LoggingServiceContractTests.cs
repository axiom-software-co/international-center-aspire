using Service.Configuration.Abstractions;
using Service.Configuration.Extensions;
using Service.Configuration.Options;
using Service.Logging.Abstractions;
using Service.Logging.Extensions;
using Service.Logging.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Sinks.TestCorrelator;
using Xunit;
using Xunit.Abstractions;

namespace Service.Logging.Tests.Integration;

/// <summary>
/// Contract-first tests for ILoggingService interface.
/// 
/// TDD PRINCIPLE: Tests drive the design of the service layer architecture
/// CONTRACT TESTING: Tests interfaces/contracts rather than implementation details  
/// PRECONDITIONS/POSTCONDITIONS: Focused on dependencies and state changes
/// 
/// MEDICAL COMPLIANCE: Validates logging service meets medical-grade requirements
/// SERVICES APIS SCOPE: Tests logging patterns for Services Public/Admin APIs
/// SERVICE LAYER: Tests service components that sit above infrastructure, below domains
/// </summary>
public sealed class LoggingServiceContractTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ServiceProvider _serviceProvider;
    private readonly ILoggingService _loggingService;

    public LoggingServiceContractTests(ITestOutputHelper output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
        
        // Arrange: Build configuration and services for testing
        var configuration = BuildTestConfiguration();
        var services = new ServiceCollection();
        
        services.AddLogging(builder => builder.AddXUnit(output));
        services.AddServiceConfiguration(configuration);
        services.AddInfrastructureLogging(configuration);
        
        _serviceProvider = services.BuildServiceProvider();
        _loggingService = _serviceProvider.GetRequiredService<ILoggingService>();
    }

    /// <summary>
    /// CONTRACT: ConfigureStructuredLogging must configure logging with correlation ID propagation
    /// 
    /// PRECONDITION: Valid logging configuration options and DI container
    /// POSTCONDITION: Structured logging configured with medical-grade compliance
    /// SERVICE LAYER: Logging service properly configured above infrastructure
    /// </summary>
    [Fact(Timeout = 5000)]
    public void ConfigureStructuredLogging_WithValidConfiguration_ShouldConfigureSuccessfully()
    {
        // Act - Configure structured logging (this should not throw)
        _loggingService.ConfigureStructuredLogging();
        
        // Assert - Environment context should indicate proper configuration
        var environmentContext = _loggingService.GetLoggingEnvironmentContext();
        Assert.NotNull(environmentContext);
        Assert.True(environmentContext.StructuredLoggingEnabled);
        Assert.True(environmentContext.CorrelationIdEnabled);
        Assert.True(environmentContext.SensitiveDataRedactionEnabled);
        Assert.True(environmentContext.MedicalComplianceLoggingEnabled);
        
        _output.WriteLine($"✅ Structured logging configured successfully in {environmentContext.Environment} environment");
    }

    /// <summary>
    /// CONTRACT: WriteStructuredLog must write log entry with correlation ID and sensitive data redaction
    /// 
    /// PRECONDITION: Valid log level, message template, and optional structured parameters
    /// POSTCONDITION: Log entry written with correlation ID and sensitive data redacted
    /// MEDICAL COMPLIANCE: PII/PHI data automatically redacted in log entries
    /// </summary>
    [Fact(Timeout = 5000)]
    public void WriteStructuredLog_WithValidParameters_ShouldWriteStructuredLogEntry()
    {
        using (TestCorrelator.CreateContext())
        {
            // Arrange
            const string messageTemplate = "Processing user request for {UserId} with {Action}";
            const string userId = "test-user-123";
            const string action = "GetProfile";
            
            // Act
            _loggingService.WriteStructuredLog(LogLevel.Information, messageTemplate, userId, action);
            
            // Assert - Log should be written (TestCorrelator captures Serilog logs)
            var logEvents = TestCorrelator.GetLogEventsFromCurrentContext();
            Assert.NotEmpty(logEvents);
            
            var logEvent = logEvents.First();
            Assert.Contains("Processing user request", logEvent.MessageTemplate.Text);
            Assert.True(logEvent.Properties.ContainsKey("CorrelationId"));
            
            _output.WriteLine($"✅ Structured log written with correlation ID: {logEvent.Properties["CorrelationId"]}");
        }
    }

    /// <summary>
    /// CONTRACT: WriteStructuredLog with exception must sanitize exception details for medical compliance
    /// 
    /// PRECONDITION: Valid exception, log level, and message template
    /// POSTCONDITION: Log entry with sanitized exception details and correlation ID
    /// MEDICAL COMPLIANCE: Exception details sanitized for PII/PHI protection
    /// </summary>
    [Fact(Timeout = 5000)]
    public void WriteStructuredLog_WithException_ShouldSanitizeExceptionDetails()
    {
        using (TestCorrelator.CreateContext())
        {
            // Arrange
            var exception = new InvalidOperationException("Test exception for medical compliance");
            const string messageTemplate = "Error occurred during {Operation}";
            const string operation = "UserProfileRetrieval";
            
            // Act
            _loggingService.WriteStructuredLog(exception, LogLevel.Error, messageTemplate, operation);
            
            // Assert
            var logEvents = TestCorrelator.GetLogEventsFromCurrentContext();
            Assert.NotEmpty(logEvents);
            
            var logEvent = logEvents.First();
            Assert.NotNull(logEvent.Exception);
            Assert.True(logEvent.Properties.ContainsKey("CorrelationId"));
            Assert.True(logEvent.Properties.ContainsKey("ExceptionType"));
            
            _output.WriteLine($"✅ Exception logged with correlation ID and type: {logEvent.Properties["ExceptionType"]}");
        }
    }

    /// <summary>
    /// CONTRACT: BeginStructuredScope must create scope with correlation ID inheritance
    /// 
    /// PRECONDITION: Valid scope name and optional structured properties
    /// POSTCONDITION: Logging scope created with correlation ID propagation
    /// SERVICE LAYER: Scope boundaries preserved across service operations
    /// </summary>
    [Fact(Timeout = 5000)]
    public void BeginStructuredScope_WithValidParameters_ShouldCreateScopeWithCorrelation()
    {
        // Arrange
        const string scopeName = "UserServiceOperation";
        var properties = new[]
        {
            ("UserId", (object)"test-user-456"),
            ("Operation", (object)"UpdateProfile")
        };
        
        // Act
        using var scope = _loggingService.BeginStructuredScope(scopeName, properties);
        
        // Assert
        Assert.NotNull(scope);
        Assert.Equal(scopeName, scope.ScopeName);
        Assert.NotEmpty(scope.CorrelationId);
        Assert.Equal(2, scope.Properties.Count);
        Assert.True(scope.Properties.ContainsKey("UserId"));
        Assert.True(scope.Properties.ContainsKey("Operation"));
        
        _output.WriteLine($"✅ Structured scope created: {scopeName} with correlation {scope.CorrelationId}");
    }

    /// <summary>
    /// CONTRACT: GetCurrentCorrelationId must return correlation ID for request tracing
    /// 
    /// POSTCONDITION: Returns current correlation ID or generates new one if none exists
    /// SERVICES APIS: Request correlation across Public Gateway -> Services APIs
    /// </summary>
    [Fact(Timeout = 5000)]
    public void GetCurrentCorrelationId_ShouldReturnValidCorrelationId()
    {
        // Act
        var correlationId = _loggingService.GetCurrentCorrelationId();
        
        // Assert
        Assert.NotNull(correlationId);
        Assert.NotEmpty(correlationId);
        Assert.NotEqual("Unknown", correlationId);
        
        _output.WriteLine($"✅ Correlation ID retrieved: {correlationId}");
    }

    /// <summary>
    /// CONTRACT: SetCorrelationId must set correlation ID for request tracing
    /// 
    /// PRECONDITION: Valid correlation ID string
    /// POSTCONDITION: Correlation ID set and retrievable for current context
    /// MEDICAL COMPLIANCE: Correlation ID validation for medical-grade audit requirements
    /// </summary>
    [Fact(Timeout = 5000)]
    public void SetCorrelationId_WithValidId_ShouldSetAndRetrieveCorrelationId()
    {
        // Arrange
        var testCorrelationId = Guid.NewGuid().ToString();
        
        // Act
        _loggingService.SetCorrelationId(testCorrelationId);
        var retrievedCorrelationId = _loggingService.GetCurrentCorrelationId();
        
        // Assert
        Assert.Equal(testCorrelationId, retrievedCorrelationId);
        
        _output.WriteLine($"✅ Correlation ID set and retrieved: {testCorrelationId}");
    }

    /// <summary>
    /// CONTRACT: SetCorrelationId must throw exception for invalid correlation IDs
    /// 
    /// PRECONDITION: Null, empty, or whitespace correlation ID
    /// POSTCONDITION: ArgumentException thrown with clear message
    /// </summary>
    [Theory(Timeout = 5000)]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetCorrelationId_WithInvalidId_ShouldThrowArgumentException(string invalidCorrelationId)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _loggingService.SetCorrelationId(invalidCorrelationId));
        
        Assert.Contains("Correlation ID cannot be null or empty", exception.Message);
        _output.WriteLine($"✅ Correctly rejected invalid correlation ID: '{invalidCorrelationId}'");
    }

    /// <summary>
    /// CONTRACT: ValidateLoggingConfiguration must validate all logging settings for medical compliance
    /// 
    /// POSTCONDITION: Returns validation result indicating compliance with medical logging requirements
    /// MEDICAL COMPLIANCE: Validates structured templates, retention policies, sensitive data redaction
    /// </summary>
    [Fact(Timeout = 5000)]
    public void ValidateLoggingConfiguration_WithValidConfiguration_ShouldReturnSuccessResult()
    {
        // Act
        var validationResult = _loggingService.ValidateLoggingConfiguration();
        
        // Assert
        Assert.NotNull(validationResult);
        Assert.True(validationResult.IsValid);
        Assert.Empty(validationResult.Errors);
        Assert.True(validationResult.ValidatedAt <= DateTime.UtcNow);
        Assert.NotEmpty(validationResult.ValidatedFeatures);
        
        // Medical compliance features should be validated
        Assert.Contains("Structured Format", validationResult.ValidatedFeatures);
        Assert.Contains("Correlation ID Propagation", validationResult.ValidatedFeatures);
        Assert.Contains("Sensitive Data Redaction", validationResult.ValidatedFeatures);
        
        _output.WriteLine($"✅ Logging configuration validation passed with {validationResult.ValidatedFeatures.Count} features");
    }

    /// <summary>
    /// CONTRACT: GetLoggingEnvironmentContext must return current logging environment information
    /// 
    /// POSTCONDITION: Returns logging context with medical compliance settings
    /// SERVICE LAYER: Environment context for service-layer logging configuration
    /// </summary>
    [Fact(Timeout = 5000)]
    public void GetLoggingEnvironmentContext_ShouldReturnEnvironmentInformation()
    {
        // Act
        var environmentContext = _loggingService.GetLoggingEnvironmentContext();
        
        // Assert
        Assert.NotNull(environmentContext);
        Assert.NotEmpty(environmentContext.Environment);
        Assert.NotEmpty(environmentContext.ConfiguredSinks);
        Assert.NotEmpty(environmentContext.MinimumLevel);
        Assert.NotNull(environmentContext.ConfigurationProperties);
        
        // Testing environment should have proper medical compliance settings
        Assert.Equal("Testing", environmentContext.Environment);
        Assert.True(environmentContext.StructuredLoggingEnabled);
        Assert.True(environmentContext.CorrelationIdEnabled);
        Assert.True(environmentContext.SensitiveDataRedactionEnabled);
        Assert.True(environmentContext.MedicalComplianceLoggingEnabled);
        Assert.True(environmentContext.LogRetentionDays >= 2555); // Medical compliance retention
        
        _output.WriteLine($"✅ Logging environment context: {environmentContext.Environment}, " +
                         $"Medical compliance: {environmentContext.MedicalComplianceLoggingEnabled}, " +
                         $"Retention: {environmentContext.LogRetentionDays} days");
    }

    /// <summary>
    /// CONTRACT: Logging service must handle sensitive data redaction in structured properties
    /// 
    /// PRECONDITION: Logging scope with sensitive property names
    /// POSTCONDITION: Sensitive data redacted in logging scope properties
    /// MEDICAL COMPLIANCE: PII/PHI protection in structured logging scopes
    /// </summary>
    [Fact(Timeout = 5000)]
    public void BeginStructuredScope_WithSensitiveData_ShouldRedactSensitiveProperties()
    {
        // Arrange
        const string scopeName = "SensitiveDataOperation";
        var properties = new[]
        {
            ("password", (object)"secret123"),
            ("normalProperty", (object)"normalValue"),
            ("medicalRecord", (object)"sensitive-medical-data")
        };
        
        // Act
        using var scope = _loggingService.BeginStructuredScope(scopeName, properties);
        
        // Assert
        Assert.NotNull(scope);
        Assert.Equal(3, scope.Properties.Count);
        
        // Sensitive properties should be redacted
        Assert.Equal("[REDACTED]", scope.Properties["password"]);
        Assert.Equal("normalValue", scope.Properties["normalProperty"]);
        Assert.Equal("[REDACTED]", scope.Properties["medicalRecord"]);
        
        _output.WriteLine($"✅ Sensitive data redacted in structured scope properties");
    }

    /// <summary>
    /// CONTRACT: Logging service must support medical-grade compliance requirements
    /// 
    /// PRECONDITION: Medical compliance logging configuration enabled
    /// POSTCONDITION: All medical compliance features are active in service layer
    /// MEDICAL COMPLIANCE: Validates service meets medical-grade standards
    /// </summary>
    [Fact(Timeout = 5000)]
    public void LoggingService_WithMedicalCompliance_ShouldEnforceComplianceRequirements()
    {
        // Act
        var environmentContext = _loggingService.GetLoggingEnvironmentContext();
        var validationResult = _loggingService.ValidateLoggingConfiguration();
        
        // Assert - Medical compliance features should be active
        Assert.True(validationResult.IsValid, "Medical compliance logging validation must pass");
        Assert.True(environmentContext.MedicalComplianceLoggingEnabled, "Medical compliance logging must be enabled");
        Assert.True(environmentContext.StructuredLoggingEnabled, "Structured logging required for medical compliance");
        Assert.True(environmentContext.SensitiveDataRedactionEnabled, "Sensitive data redaction required for medical compliance");
        Assert.True(environmentContext.CorrelationIdEnabled, "Correlation IDs required for medical compliance");
        
        // Log retention must meet medical standards
        Assert.True(environmentContext.LogRetentionDays >= 2555, "Log retention must meet medical standards (~7 years)");
        
        // Validation should include medical compliance features
        Assert.Contains("Medical Log Retention", validationResult.ValidatedFeatures);
        
        _output.WriteLine($"✅ Medical compliance requirements validated successfully in service layer");
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
            // Test structured logging configuration
            ["StructuredLogging:EnableStructuredFormat"] = "true",
            ["StructuredLogging:MessageTemplates:HttpRequest"] = "HTTP {RequestMethod} {RequestPath} started with correlation {CorrelationId}",
            ["StructuredLogging:MessageTemplates:HttpResponse"] = "HTTP {RequestMethod} {RequestPath} completed in {ElapsedMs}ms with status {StatusCode}",
            ["StructuredLogging:MessageTemplates:MedicalAudit"] = "Medical audit {AuditAction} for entity {EntityType}:{EntityId} by user {UserId}",
            
            // Sensitive data redaction configuration
            ["StructuredLogging:SensitiveDataRedaction:EnableRedaction"] = "true",
            ["StructuredLogging:SensitiveDataRedaction:RedactionPlaceholder"] = "[REDACTED]",
            ["StructuredLogging:SensitiveDataRedaction:MaxRedactedLength"] = "50",
            
            // Correlation ID configuration
            ["StructuredLogging:CorrelationId:EnableCorrelationId"] = "true",
            ["StructuredLogging:CorrelationId:HeaderName"] = "X-Correlation-ID",
            ["StructuredLogging:CorrelationId:GenerateIfMissing"] = "true",
            ["StructuredLogging:CorrelationId:Format"] = "0", // Guid format
            
            // Performance logging configuration
            ["StructuredLogging:PerformanceLogging:EnablePerformanceLogging"] = "true",
            ["StructuredLogging:PerformanceLogging:SlowRequestThresholdMs"] = "1000",
            
            // Test logging configuration (from Service.Configuration)
            ["Logging:MinimumLevel"] = "Information",
            ["Logging:EnableStructuredLogging"] = "true",
            ["Logging:EnableCorrelationIds"] = "true",
            ["Logging:EnableSensitiveDataRedaction"] = "true",
            ["Logging:LogRetentionDays"] = "2555",
            
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