using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;
using InternationalCenter.Shared.Tests.Abstractions;

namespace InternationalCenter.Shared.Tests.Infrastructure;

/// <summary>
/// Concrete implementation of validation utilities for test operations
/// Provides comprehensive validation patterns with medical-grade compliance
/// Contract-first validation ensuring consistent patterns across all test domains
/// </summary>
public class ValidationUtilities : IValidationUtilities
{
    private readonly ILogger<ValidationUtilities> _logger;
    private static readonly string[] SensitivePatterns =
    [
        @"password\s*[:=]\s*['""]?([^'""]+)['""]?",
        @"secret\s*[:=]\s*['""]?([^'""]+)['""]?",
        @"key\s*[:=]\s*['""]?([^'""]+)['""]?",
        @"token\s*[:=]\s*['""]?([^'""]+)['""]?",
        @"api[_-]?key\s*[:=]\s*['""]?([^'""]+)['""]?",
        @"connection[_-]?string\s*[:=]\s*['""]?([^'""]+)['""]?",
        @"\b\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}\b", // Credit card patterns
        @"\b\d{3}-\d{2}-\d{4}\b", // SSN patterns
        @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}" // Email patterns in error messages
    ];

    public ValidationUtilities(ILogger<ValidationUtilities> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates HTTP response security headers for medical-grade compliance
    /// Contract: Must validate all required security headers and log violations
    /// </summary>
    public async Task ValidateSecurityHeadersAsync(
        HttpResponseMessage response,
        SecurityHeaderRequirements? requirements = null,
        ITestOutputHelper? output = null)
    {
        ArgumentNullException.ThrowIfNull(response);

        requirements ??= new SecurityHeaderRequirements
        {
            RequiredHeaders = [
                "X-Content-Type-Options",
                "X-Frame-Options",
                "X-XSS-Protection",
                "Strict-Transport-Security",
                "Content-Security-Policy"
            ],
            StrictValidation = true,
            ExpectedHeaderValues = new Dictionary<string, string[]>
            {
                ["X-Content-Type-Options"] = ["nosniff"],
                ["X-Frame-Options"] = ["DENY", "SAMEORIGIN"],
                ["X-XSS-Protection"] = ["1; mode=block", "0"]
            }
        };

        var violations = new List<string>();
        var headers = response.Headers.Concat(response.Content.Headers);

        // Check required headers
        foreach (var requiredHeader in requirements.RequiredHeaders)
        {
            var headerValues = headers
                .Where(h => string.Equals(h.Key, requiredHeader, StringComparison.OrdinalIgnoreCase))
                .SelectMany(h => h.Value)
                .ToArray();

            if (!headerValues.Any())
            {
                violations.Add($"Missing required security header: {requiredHeader}");
                continue;
            }

            // Validate header values if specified
            if (requirements.ExpectedHeaderValues.TryGetValue(requiredHeader, out var expectedValues))
            {
                var hasValidValue = headerValues.Any(value => 
                    expectedValues.Any(expected => 
                        string.Equals(value, expected, StringComparison.OrdinalIgnoreCase)));

                if (!hasValidValue)
                {
                    violations.Add($"Invalid value for header {requiredHeader}: {string.Join(", ", headerValues)}. Expected one of: {string.Join(", ", expectedValues)}");
                }
            }
        }

        // Log and handle violations
        if (violations.Any())
        {
            var violationMessage = string.Join("; ", violations);
            _logger.LogWarning("Security header violations detected: {Violations}", violationMessage);
            output?.WriteLine($"Security header violations: {violationMessage}");

            if (requirements.StrictValidation)
            {
                throw new ValidationContractViolationException(
                    "SecurityHeaders",
                    $"All required security headers present with valid values",
                    violationMessage);
            }
        }
        else
        {
            _logger.LogDebug("All security header validations passed");
            output?.WriteLine("All security header validations passed");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Validates HTTP response correlation tracking for audit compliance
    /// Contract: Must ensure correlation headers are present for medical-grade audit trails
    /// </summary>
    public async Task ValidateCorrelationTrackingAsync(
        HttpResponseMessage response,
        CorrelationRequirements? requirements = null,
        ITestOutputHelper? output = null)
    {
        ArgumentNullException.ThrowIfNull(response);

        requirements ??= new CorrelationRequirements();
        var violations = new List<string>();
        var headers = response.Headers.Concat(response.Content.Headers);

        if (requirements.RequireCorrelationId)
        {
            var correlationHeaders = headers
                .Where(h => requirements.AcceptableCorrelationHeaders.Any(acceptable =>
                    string.Equals(h.Key, acceptable, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            if (!correlationHeaders.Any())
            {
                violations.Add($"Missing correlation header. Expected one of: {string.Join(", ", requirements.AcceptableCorrelationHeaders)}");
            }
            else
            {
                // Validate correlation ID format if validator provided
                if (requirements.CorrelationIdValidator != null)
                {
                    var correlationValues = correlationHeaders.SelectMany(h => h.Value);
                    var hasValidCorrelationId = correlationValues.Any(requirements.CorrelationIdValidator);

                    if (!hasValidCorrelationId)
                    {
                        violations.Add("Correlation ID format validation failed");
                    }
                }
            }
        }

        // Log and handle violations
        if (violations.Any())
        {
            var violationMessage = string.Join("; ", violations);
            _logger.LogWarning("Correlation tracking violations detected: {Violations}", violationMessage);
            output?.WriteLine($"Correlation tracking violations: {violationMessage}");

            throw new ValidationContractViolationException(
                "CorrelationTracking",
                "Proper correlation headers present for audit trail",
                violationMessage);
        }
        else
        {
            _logger.LogDebug("Correlation tracking validation passed");
            output?.WriteLine("Correlation tracking validation passed");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Validates anonymous access patterns for public endpoints
    /// Contract: Must ensure proper anonymous access without authentication requirements
    /// </summary>
    public async Task ValidateAnonymousAccessAsync(
        HttpResponseMessage response,
        ITestOutputHelper? output = null)
    {
        ArgumentNullException.ThrowIfNull(response);

        var violations = new List<string>();

        // Validate that authentication is not required
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            violations.Add("Anonymous access denied - endpoint requires authentication");
        }

        // Check for authentication-related headers that shouldn't be present for anonymous access
        var authHeaders = response.Headers
            .Where(h => string.Equals(h.Key, "WWW-Authenticate", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (authHeaders.Any() && response.StatusCode != HttpStatusCode.Unauthorized)
        {
            violations.Add("Unexpected WWW-Authenticate header present for anonymous access");
        }

        // Validate that the response is appropriate for anonymous access
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            violations.Add("Anonymous access forbidden - may indicate authorization issues");
        }

        // Log and handle violations
        if (violations.Any())
        {
            var violationMessage = string.Join("; ", violations);
            _logger.LogWarning("Anonymous access violations detected: {Violations}", violationMessage);
            output?.WriteLine($"Anonymous access violations: {violationMessage}");

            throw new ValidationContractViolationException(
                "AnonymousAccess",
                "Endpoint allows proper anonymous access",
                violationMessage);
        }
        else
        {
            _logger.LogDebug("Anonymous access validation passed");
            output?.WriteLine("Anonymous access validation passed");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Validates error response safety ensuring no sensitive information leakage
    /// Contract: Must detect and report any sensitive data in error responses
    /// </summary>
    public async Task ValidateErrorResponseSafetyAsync<T>(
        IResult<T> result,
        ITestOutputHelper? output = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess || result.Error == null)
        {
            _logger.LogDebug("Result is successful, skipping error response safety validation");
            return;
        }

        var violations = new List<string>();
        var errorContent = JsonSerializer.Serialize(new
        {
            Code = result.Error.Code,
            Message = result.Error.Message,
            Details = result.Error.Details
        });

        // Check for sensitive information patterns
        foreach (var pattern in SensitivePatterns)
        {
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            var matches = regex.Matches(errorContent);

            foreach (Match match in matches)
            {
                violations.Add($"Potential sensitive data leak detected: pattern '{pattern}' matched '{match.Value}'");
            }
        }

        // Check for stack traces in production-like responses
        if (errorContent.Contains("StackTrace", StringComparison.OrdinalIgnoreCase) ||
            errorContent.Contains("at System.", StringComparison.OrdinalIgnoreCase) ||
            errorContent.Contains("   at ", StringComparison.OrdinalIgnoreCase))
        {
            violations.Add("Stack trace information detected in error response");
        }

        // Check for internal file paths
        if (Regex.IsMatch(errorContent, @"[A-Za-z]:\\[^\\]+\\", RegexOptions.IgnoreCase) ||
            Regex.IsMatch(errorContent, @"/[a-zA-Z0-9_-]+/[a-zA-Z0-9_/-]+", RegexOptions.IgnoreCase))
        {
            violations.Add("Internal file path detected in error response");
        }

        // Log and handle violations
        if (violations.Any())
        {
            var violationMessage = string.Join("; ", violations);
            _logger.LogError("Error response safety violations detected: {Violations}", violationMessage);
            output?.WriteLine($"ERROR: Sensitive data leak detected: {violationMessage}");

            throw new ValidationContractViolationException(
                "ErrorResponseSafety",
                "Error response contains no sensitive information",
                violationMessage);
        }
        else
        {
            _logger.LogDebug("Error response safety validation passed");
            output?.WriteLine("Error response safety validation passed");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Validates business rule violation responses
    /// Contract: Must ensure proper business rule error codes and messages
    /// </summary>
    public async Task ValidateBusinessRuleViolationAsync<T>(
        IResult<T> result,
        string expectedBusinessRule,
        ITestOutputHelper? output = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedBusinessRule);

        if (result.IsSuccess)
        {
            _logger.LogWarning("Expected business rule violation but result was successful");
            output?.WriteLine($"WARNING: Expected business rule violation '{expectedBusinessRule}' but result was successful");

            throw new ValidationContractViolationException(
                "BusinessRuleViolation",
                $"Business rule violation for '{expectedBusinessRule}'",
                "Result was successful");
        }

        if (result.Error == null)
        {
            throw new ValidationContractViolationException(
                "BusinessRuleViolation",
                "Error object present for business rule violation",
                "Error object is null");
        }

        var violations = new List<string>();

        // Validate error code format (should be descriptive and follow naming conventions)
        if (string.IsNullOrWhiteSpace(result.Error.Code))
        {
            violations.Add("Business rule error code is missing or empty");
        }
        else if (!Regex.IsMatch(result.Error.Code, @"^[A-Z][A-Za-z0-9_]+$"))
        {
            violations.Add($"Business rule error code '{result.Error.Code}' does not follow naming conventions");
        }

        // Validate error message is descriptive and user-friendly
        if (string.IsNullOrWhiteSpace(result.Error.Message))
        {
            violations.Add("Business rule error message is missing or empty");
        }
        else if (result.Error.Message.Length < 10)
        {
            violations.Add("Business rule error message is too short to be descriptive");
        }

        // Validate that the error relates to the expected business rule
        var errorContent = $"{result.Error.Code} {result.Error.Message} {result.Error.Details}";
        if (!errorContent.Contains(expectedBusinessRule, StringComparison.OrdinalIgnoreCase))
        {
            violations.Add($"Error does not appear to relate to expected business rule '{expectedBusinessRule}'");
        }

        // Log and handle violations
        if (violations.Any())
        {
            var violationMessage = string.Join("; ", violations);
            _logger.LogWarning("Business rule validation violations detected: {Violations}", violationMessage);
            output?.WriteLine($"Business rule validation violations: {violationMessage}");

            throw new ValidationContractViolationException(
                "BusinessRuleViolation",
                $"Proper business rule violation for '{expectedBusinessRule}'",
                violationMessage);
        }
        else
        {
            _logger.LogDebug("Business rule validation passed for '{ExpectedRule}'", expectedBusinessRule);
            output?.WriteLine($"Business rule validation passed for '{expectedBusinessRule}'");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Validates null input handling across all contract implementations
    /// Contract: Must ensure consistent null input validation and error responses
    /// </summary>
    public async Task ValidateNullInputHandlingAsync<T>(
        IResult<T> result,
        string parameterName,
        ITestOutputHelper? output = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);

        if (result.IsSuccess)
        {
            throw new ValidationContractViolationException(
                "NullInputHandling",
                $"Null input rejection for parameter '{parameterName}'",
                "Result was successful");
        }

        if (result.Error == null)
        {
            throw new ValidationContractViolationException(
                "NullInputHandling",
                "Error object present for null input",
                "Error object is null");
        }

        var violations = new List<string>();

        // Validate error code indicates null input
        var expectedErrorPatterns = new[]
        {
            "ArgumentNull",
            "NullParameter",
            "RequiredParameter",
            "InvalidInput"
        };

        if (!expectedErrorPatterns.Any(pattern =>
            result.Error.Code.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
        {
            violations.Add($"Error code '{result.Error.Code}' does not indicate null input validation");
        }

        // Validate error message mentions the parameter
        var errorContent = $"{result.Error.Message} {result.Error.Details}";
        if (!errorContent.Contains(parameterName, StringComparison.OrdinalIgnoreCase))
        {
            violations.Add($"Error message does not mention parameter '{parameterName}'");
        }

        // Validate error message is user-friendly
        if (string.IsNullOrWhiteSpace(result.Error.Message) || result.Error.Message.Length < 10)
        {
            violations.Add("Error message is missing or too short for null input validation");
        }

        // Log and handle violations
        if (violations.Any())
        {
            var violationMessage = string.Join("; ", violations);
            _logger.LogWarning("Null input handling violations detected: {Violations}", violationMessage);
            output?.WriteLine($"Null input handling violations: {violationMessage}");

            throw new ValidationContractViolationException(
                "NullInputHandling",
                $"Proper null input handling for parameter '{parameterName}'",
                violationMessage);
        }
        else
        {
            _logger.LogDebug("Null input handling validation passed for parameter '{ParameterName}'", parameterName);
            output?.WriteLine($"Null input handling validation passed for parameter '{parameterName}'");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Validates medical-grade audit context in requests
    /// Contract: Must ensure all required audit properties are present and valid
    /// </summary>
    public async Task ValidateMedicalGradeAuditContextAsync<TRequest>(
        TRequest request,
        AuditContextRequirements? requirements = null,
        ITestOutputHelper? output = null)
        where TRequest : class
    {
        ArgumentNullException.ThrowIfNull(request);

        requirements ??= new AuditContextRequirements();
        var violations = new List<string>();

        // Serialize request to analyze properties
        var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        var requestData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(requestJson) ?? new Dictionary<string, JsonElement>();

        // Check required audit properties
        foreach (var requiredProperty in requirements.RequiredAuditProperties)
        {
            if (!requestData.ContainsKey(requiredProperty))
            {
                violations.Add($"Missing required audit property: {requiredProperty}");
            }
        }

        // Validate user context if required
        if (requirements.RequireUserContext)
        {
            var userContextKeys = new[] { "userId", "userName", "userEmail", "userContext", "performedBy" };
            if (!userContextKeys.Any(key => requestData.ContainsKey(key)))
            {
                violations.Add("Missing user context information for audit trail");
            }
        }

        // Validate timestamp if required
        if (requirements.RequireTimestamp)
        {
            var timestampKeys = new[] { "timestamp", "createdAt", "performedAt", "auditTimestamp" };
            if (!timestampKeys.Any(key => requestData.ContainsKey(key)))
            {
                violations.Add("Missing timestamp information for audit trail");
            }
        }

        // Validate correlation ID if required
        if (requirements.RequireCorrelationId)
        {
            var correlationKeys = new[] { "correlationId", "traceId", "requestId", "auditId" };
            if (!correlationKeys.Any(key => requestData.ContainsKey(key)))
            {
                violations.Add("Missing correlation ID for audit trail");
            }
        }

        // Log and handle violations
        if (violations.Any())
        {
            var violationMessage = string.Join("; ", violations);
            _logger.LogError("Medical-grade audit context violations detected: {Violations}", violationMessage);
            output?.WriteLine($"Medical-grade audit violations: {violationMessage}");

            throw new ValidationContractViolationException(
                "MedicalGradeAudit",
                "Complete audit context present for medical-grade compliance",
                violationMessage);
        }
        else
        {
            _logger.LogDebug("Medical-grade audit context validation passed");
            output?.WriteLine("Medical-grade audit context validation passed");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Validates entity integrity against domain rules
    /// Contract: Must validate all domain invariants and business rules
    /// </summary>
    public async Task ValidateEntityIntegrityAsync<TEntity>(
        TEntity entity,
        EntityValidationRules<TEntity>? rules = null,
        ITestOutputHelper? output = null)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(entity);

        rules ??= new EntityValidationRules<TEntity>();
        var violations = new List<string>();

        // Validate invariants
        foreach (var invariant in rules.Invariants)
        {
            try
            {
                if (!invariant(entity))
                {
                    violations.Add($"Entity invariant violation detected");
                }
            }
            catch (Exception ex)
            {
                violations.Add($"Entity invariant validation failed: {ex.Message}");
            }
        }

        // Validate async validators
        foreach (var asyncValidator in rules.AsyncValidators)
        {
            try
            {
                if (!await asyncValidator(entity))
                {
                    violations.Add($"Entity async validation failed");
                }
            }
            catch (Exception ex)
            {
                violations.Add($"Entity async validation error: {ex.Message}");
            }
        }

        // Validate individual properties
        var entityProperties = JsonSerializer.Serialize(entity);
        var propertyData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(entityProperties) ?? new Dictionary<string, JsonElement>();

        foreach (var (propertyName, propertyValidator) in rules.PropertyValidators)
        {
            try
            {
                if (!propertyValidator(entity))
                {
                    violations.Add($"Property validation failed for: {propertyName}");
                }
            }
            catch (Exception ex)
            {
                violations.Add($"Property validation error for {propertyName}: {ex.Message}");
            }
        }

        // Log and handle violations
        if (violations.Any())
        {
            var violationMessage = string.Join("; ", violations);
            _logger.LogWarning("Entity integrity violations detected: {Violations}", violationMessage);
            output?.WriteLine($"Entity integrity violations: {violationMessage}");

            throw new ValidationContractViolationException(
                "EntityIntegrity",
                "Entity passes all domain invariants and business rules",
                violationMessage);
        }
        else
        {
            _logger.LogDebug("Entity integrity validation passed for {EntityType}", typeof(TEntity).Name);
            output?.WriteLine($"Entity integrity validation passed for {typeof(TEntity).Name}");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Validates relationship integrity between entities
    /// Contract: Must ensure referential integrity and relationship constraints
    /// </summary>
    public async Task ValidateRelationshipIntegrityAsync<TParent, TChild>(
        TParent parent,
        TChild child,
        RelationshipValidationRules<TParent, TChild>? rules = null,
        ITestOutputHelper? output = null)
        where TParent : class
        where TChild : class
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(child);

        rules ??= new RelationshipValidationRules<TParent, TChild>();
        var violations = new List<string>();

        // Validate relationship invariants
        foreach (var invariant in rules.RelationshipInvariants)
        {
            try
            {
                if (!invariant(parent, child))
                {
                    violations.Add($"Relationship invariant violation between {typeof(TParent).Name} and {typeof(TChild).Name}");
                }
            }
            catch (Exception ex)
            {
                violations.Add($"Relationship invariant validation failed: {ex.Message}");
            }
        }

        // Validate async relationship validators
        foreach (var asyncValidator in rules.AsyncRelationshipValidators)
        {
            try
            {
                if (!await asyncValidator(parent, child))
                {
                    violations.Add($"Relationship async validation failed between {typeof(TParent).Name} and {typeof(TChild).Name}");
                }
            }
            catch (Exception ex)
            {
                violations.Add($"Relationship async validation error: {ex.Message}");
            }
        }

        // Log and handle violations
        if (violations.Any())
        {
            var violationMessage = string.Join("; ", violations);
            _logger.LogWarning("Relationship integrity violations detected: {Violations}", violationMessage);
            output?.WriteLine($"Relationship integrity violations: {violationMessage}");

            throw new ValidationContractViolationException(
                "RelationshipIntegrity",
                $"Valid relationship between {typeof(TParent).Name} and {typeof(TChild).Name}",
                violationMessage);
        }
        else
        {
            _logger.LogDebug("Relationship integrity validation passed between {ParentType} and {ChildType}",
                typeof(TParent).Name, typeof(TChild).Name);
            output?.WriteLine($"Relationship integrity validation passed between {typeof(TParent).Name} and {typeof(TChild).Name}");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Validates performance contracts for operations
    /// Contract: Must throw PerformanceContractViolationException for violations
    /// </summary>
    public async Task ValidatePerformanceContractAsync(
        string operationName,
        TimeSpan actualDuration,
        TimeSpan expectedMaxDuration,
        ITestOutputHelper? output = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        if (actualDuration > expectedMaxDuration)
        {
            var violationMessage = $"Performance contract violation: {operationName} took {actualDuration.TotalMilliseconds:F0}ms, expected max {expectedMaxDuration.TotalMilliseconds:F0}ms";
            
            _logger.LogError("Performance contract violation detected: {Violation}", violationMessage);
            output?.WriteLine($"PERFORMANCE VIOLATION: {violationMessage}");

            throw new PerformanceContractViolationException(operationName, actualDuration, expectedMaxDuration);
        }
        else
        {
            _logger.LogDebug("Performance contract validation passed for {OperationName}: {ActualDuration}ms <= {MaxDuration}ms",
                operationName, actualDuration.TotalMilliseconds, expectedMaxDuration.TotalMilliseconds);
            output?.WriteLine($"Performance validation passed for {operationName}: {actualDuration.TotalMilliseconds:F0}ms");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Validates test data quality and realism
    /// Contract: Must ensure generated test data follows realistic patterns
    /// </summary>
    public async Task ValidateTestDataQualityAsync<TEntity>(
        TEntity entity,
        TestDataQualityRules<TEntity>? rules = null,
        ITestOutputHelper? output = null)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(entity);

        rules ??= new TestDataQualityRules<TEntity>();
        var violations = new List<string>();

        // Check if data is realistic
        try
        {
            if (!rules.IsRealistic(entity))
            {
                violations.Add("Test data does not appear realistic");
            }
        }
        catch (Exception ex)
        {
            violations.Add($"Realism validation error: {ex.Message}");
        }

        // Check if data follows domain rules
        try
        {
            if (!rules.FollowsDomainRules(entity))
            {
                violations.Add("Test data does not follow domain rules");
            }
        }
        catch (Exception ex)
        {
            violations.Add($"Domain rules validation error: {ex.Message}");
        }

        // Check if data has required properties
        try
        {
            if (!rules.HasRequiredProperties(entity))
            {
                violations.Add("Test data is missing required properties");
            }
        }
        catch (Exception ex)
        {
            violations.Add($"Required properties validation error: {ex.Message}");
        }

        // Check for forbidden test patterns
        var entityJson = JsonSerializer.Serialize(entity);
        foreach (var forbiddenPattern in rules.ForbiddenTestPatterns)
        {
            if (entityJson.Contains(forbiddenPattern, StringComparison.OrdinalIgnoreCase))
            {
                violations.Add($"Test data contains forbidden pattern: {forbiddenPattern}");
            }
        }

        // Log and handle violations
        if (violations.Any())
        {
            var violationMessage = string.Join("; ", violations);
            _logger.LogWarning("Test data quality violations detected: {Violations}", violationMessage);
            output?.WriteLine($"Test data quality violations: {violationMessage}");

            throw new ValidationContractViolationException(
                "TestDataQuality",
                "Test data is realistic and follows domain rules",
                violationMessage);
        }
        else
        {
            _logger.LogDebug("Test data quality validation passed for {EntityType}", typeof(TEntity).Name);
            output?.WriteLine($"Test data quality validation passed for {typeof(TEntity).Name}");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Validates contract postconditions after operation execution
    /// Contract: Must verify all expected postconditions are met
    /// </summary>
    public async Task ValidatePostconditionsAsync<TResult>(
        TResult result,
        Func<TResult, bool> postconditionCheck,
        string operationName,
        string postconditionDescription,
        ITestOutputHelper? output = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(postconditionCheck);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(postconditionDescription);

        try
        {
            var postconditionMet = postconditionCheck(result);
            
            if (!postconditionMet)
            {
                var violationMessage = $"Postcondition '{postconditionDescription}' not met for operation '{operationName}'";
                
                _logger.LogError("Postcondition violation detected: {Violation}", violationMessage);
                output?.WriteLine($"POSTCONDITION VIOLATION: {violationMessage}");

                throw new ValidationContractViolationException(
                    "Postcondition",
                    postconditionDescription,
                    "Postcondition not met");
            }
            else
            {
                _logger.LogDebug("Postcondition validation passed for {OperationName}: {PostconditionDescription}",
                    operationName, postconditionDescription);
                output?.WriteLine($"Postcondition validation passed for {operationName}: {postconditionDescription}");
            }
        }
        catch (ValidationContractViolationException)
        {
            throw; // Re-throw validation contract violations
        }
        catch (Exception ex)
        {
            var violationMessage = $"Postcondition validation error for operation '{operationName}': {ex.Message}";
            
            _logger.LogError(ex, "Postcondition validation error: {Violation}", violationMessage);
            output?.WriteLine($"POSTCONDITION ERROR: {violationMessage}");

            throw new ValidationContractViolationException(
                "Postcondition",
                postconditionDescription,
                violationMessage);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Validates contract preconditions before operation execution
    /// Contract: Must verify all required preconditions are satisfied
    /// </summary>
    public async Task ValidatePreconditionsAsync<TInput>(
        TInput input,
        IEnumerable<Func<TInput, bool>> preconditionChecks,
        string operationName,
        ITestOutputHelper? output = null)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(preconditionChecks);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        var violations = new List<string>();
        var checkIndex = 0;

        foreach (var preconditionCheck in preconditionChecks)
        {
            checkIndex++;
            try
            {
                var preconditionMet = preconditionCheck(input);
                
                if (!preconditionMet)
                {
                    violations.Add($"Precondition #{checkIndex} not met for operation '{operationName}'");
                }
            }
            catch (Exception ex)
            {
                violations.Add($"Precondition #{checkIndex} validation error: {ex.Message}");
            }
        }

        // Log and handle violations
        if (violations.Any())
        {
            var violationMessage = string.Join("; ", violations);
            _logger.LogError("Precondition violations detected: {Violations}", violationMessage);
            output?.WriteLine($"PRECONDITION VIOLATIONS: {violationMessage}");

            throw new ValidationContractViolationException(
                "Precondition",
                $"All preconditions met for operation '{operationName}'",
                violationMessage);
        }
        else
        {
            _logger.LogDebug("Precondition validation passed for {OperationName} ({CheckCount} checks)",
                operationName, checkIndex);
            output?.WriteLine($"Precondition validation passed for {operationName} ({checkIndex} checks)");
        }

        await Task.CompletedTask;
    }
}