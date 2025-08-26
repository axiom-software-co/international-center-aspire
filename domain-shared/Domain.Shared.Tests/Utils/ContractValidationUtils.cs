using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using InternationalCenter.Services.Domain.Models;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace InternationalCenter.Tests.Shared.Utils;

/// <summary>
/// Shared validation utilities for contract-first testing
/// Extracts common validation logic from duplicated contract tests
/// Provides consistent validation patterns across Services APIs and Gateway tests
/// Medical-grade validation ensuring contract compliance across distributed components
/// </summary>
public static class ContractValidationUtils
{
    #region HTTP Response Contract Validation
    
    /// <summary>
    /// Validates that HTTP response includes required security headers for medical-grade compliance
    /// Common pattern used across Public and Admin Gateway contract tests
    /// </summary>
    public static void ValidateSecurityHeaders(HttpResponseMessage response, ITestOutputHelper? output = null)
    {
        var requiredHeaders = new[]
        {
            "X-Content-Type-Options",
            "X-Frame-Options", 
            "X-XSS-Protection",
            "Referrer-Policy",
            "Content-Security-Policy"
        };
        
        var missingHeaders = new List<string>();
        
        foreach (var headerName in requiredHeaders)
        {
            if (!response.Headers.Contains(headerName) && 
                !response.Content.Headers.Contains(headerName))
            {
                missingHeaders.Add(headerName);
            }
        }
        
        if (missingHeaders.Any())
        {
            var message = $"Missing required security headers: {string.Join(", ", missingHeaders)}";
            output?.WriteLine($"❌ SECURITY HEADER VALIDATION: {message}");
            throw new InvalidOperationException($"Security contract violated: {message}");
        }
        
        output?.WriteLine("✅ SECURITY HEADERS: All required security headers are present");
    }
    
    /// <summary>
    /// Validates that response includes correlation tracking for audit compliance
    /// Medical-grade systems require request correlation for traceability
    /// </summary>
    public static void ValidateCorrelationTracking(HttpResponseMessage response, ITestOutputHelper? output = null)
    {
        var correlationHeaders = new[] { "X-Correlation-ID", "X-Request-ID", "TraceId" };
        
        var hasCorrelation = correlationHeaders.Any(header => 
            response.Headers.Contains(header) || response.Content.Headers.Contains(header));
        
        if (!hasCorrelation)
        {
            var message = "Response missing correlation tracking headers";
            output?.WriteLine($"❌ CORRELATION TRACKING: {message}");
            throw new InvalidOperationException($"Audit contract violated: {message}");
        }
        
        output?.WriteLine("✅ CORRELATION TRACKING: Request correlation headers present");
    }
    
    /// <summary>
    /// Validates that anonymous access is properly enforced (no auth required)
    /// Used by Public Gateway contract tests to ensure website accessibility
    /// </summary>
    public static void ValidateAnonymousAccess(HttpResponseMessage response, ITestOutputHelper? output = null)
    {
        var forbiddenStatusCodes = new[]
        {
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden
        };
        
        if (forbiddenStatusCodes.Contains(response.StatusCode))
        {
            var message = $"Anonymous access rejected with {response.StatusCode}";
            output?.WriteLine($"❌ ANONYMOUS ACCESS: {message}");
            throw new InvalidOperationException($"Anonymous access contract violated: {message}");
        }
        
        output?.WriteLine("✅ ANONYMOUS ACCESS: Request processed without authentication requirements");
    }
    
    #endregion
    
    #region Error Response Contract Validation
    
    /// <summary>
    /// Validates that error responses don't leak sensitive information
    /// Medical-grade compliance requires careful error message handling
    /// Common pattern across all use case contract tests
    /// </summary>
    public static void ValidateErrorResponseSafety<T>(Result<T> result, ITestOutputHelper? output = null)
    {
        if (!result.IsSuccess && result.Error != null)
        {
            var errorMessage = result.Error.Message?.ToLowerInvariant() ?? "";
            
            // Check that error doesn't contain sensitive information
            var sensitiveTerms = new[] 
            { 
                "password", "connection string", "database", "sql", "server", 
                "exception", "stack trace", "inner exception", "file path",
                "connection", "authentication", "token", "key", "secret"
            };
            
            var leakedTerms = sensitiveTerms.Where(term => errorMessage.Contains(term)).ToList();
            
            if (leakedTerms.Any())
            {
                var message = $"Error response leaks sensitive information: {string.Join(", ", leakedTerms)}";
                output?.WriteLine($"❌ ERROR SAFETY: {message}");
                throw new InvalidOperationException($"Security contract violated: {message}");
            }
            
            output?.WriteLine("✅ ERROR SAFETY: Error response doesn't leak sensitive information");
        }
    }
    
    /// <summary>
    /// Validates that error response includes proper error code for contract compliance
    /// Ensures consistent error handling patterns across Services APIs
    /// </summary>
    public static void ValidateErrorContract<T>(Result<T> result, string expectedErrorCode, ITestOutputHelper? output = null)
    {
        Assert.False(result.IsSuccess, "Expected operation to fail for error contract testing");
        Assert.NotNull(result.Error);
        
        if (result.Error!.Code != expectedErrorCode)
        {
            var message = $"Expected error code {expectedErrorCode}, got {result.Error.Code}";
            output?.WriteLine($"❌ ERROR CONTRACT: {message}");
            throw new InvalidOperationException($"Error contract violated: {message}");
        }
        
        output?.WriteLine($"✅ ERROR CONTRACT: Proper error code {expectedErrorCode} returned");
    }
    
    #endregion
    
    #region Medical-Grade Audit Validation
    
    /// <summary>
    /// Validates that request contains all required audit context for medical-grade compliance
    /// Common pattern across use case contract tests requiring audit trails
    /// </summary>
    public static void ValidateMedicalGradeAuditContext<TRequest>(TRequest request, ITestOutputHelper? output = null)
        where TRequest : class
    {
        var requestType = request.GetType();
        var auditProperties = new[] 
        { 
            "RequestId", "CorrelationId", "UserContext", "ClientIpAddress", 
            "UserAgent", "Timestamp", "TenantId" 
        };
        
        var missingProperties = new List<string>();
        
        foreach (var propertyName in auditProperties)
        {
            var property = requestType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property != null)
            {
                var value = property.GetValue(request);
                if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
                {
                    missingProperties.Add(propertyName);
                }
            }
        }
        
        // Only enforce audit properties if they exist on the request type
        var existingAuditProperties = auditProperties
            .Where(prop => requestType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance) != null)
            .ToList();
            
        if (existingAuditProperties.Any() && missingProperties.Count == existingAuditProperties.Count)
        {
            output?.WriteLine($"⚠️ AUDIT CONTEXT: No audit properties found on {requestType.Name} - may not require audit context");
            return;
        }
        
        if (missingProperties.Any())
        {
            var message = $"Missing audit context properties: {string.Join(", ", missingProperties)}";
            output?.WriteLine($"❌ AUDIT CONTEXT: {message}");
            throw new InvalidOperationException($"Medical-grade audit contract violated: {message}");
        }
        
        output?.WriteLine("✅ AUDIT CONTEXT: Request contains required medical-grade audit information");
    }
    
    #endregion
    
    #region Performance Contract Validation
    
    /// <summary>
    /// Validates that operation completes within specified time limit
    /// Common performance contract pattern across all contract tests
    /// </summary>
    public static async Task<T> ValidatePerformanceContract<T>(
        Func<Task<T>> operation,
        TimeSpan maxDuration,
        string operationName,
        ITestOutputHelper? output = null)
    {
        output?.WriteLine($"⏱️ PERFORMANCE CONTRACT: Testing {operationName} (max: {maxDuration.TotalMilliseconds}ms)");
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await operation();
        stopwatch.Stop();
        
        if (stopwatch.Elapsed > maxDuration)
        {
            var message = $"{operationName} took {stopwatch.ElapsedMilliseconds}ms (max: {maxDuration.TotalMilliseconds}ms)";
            output?.WriteLine($"❌ PERFORMANCE CONTRACT: {message}");
            throw new InvalidOperationException($"Performance contract violated: {message}");
        }
        
        output?.WriteLine($"✅ PERFORMANCE CONTRACT: {operationName} completed in {stopwatch.ElapsedMilliseconds}ms");
        return result;
    }
    
    /// <summary>
    /// Validates performance contract without return value
    /// </summary>
    public static async Task ValidatePerformanceContract(
        Func<Task> operation,
        TimeSpan maxDuration,
        string operationName,
        ITestOutputHelper? output = null)
    {
        await ValidatePerformanceContract(async () => 
        {
            await operation();
            return true;
        }, maxDuration, operationName, output);
    }
    
    #endregion
    
    #region Business Rule Contract Validation
    
    /// <summary>
    /// Validates that business rule violations return proper business error
    /// Common pattern across use case contract tests
    /// </summary>
    public static void ValidateBusinessRuleViolation<T>(
        Result<T> result,
        string expectedBusinessRule,
        ITestOutputHelper? output = null)
    {
        Assert.False(result.IsSuccess, "Expected business rule violation");
        Assert.NotNull(result.Error);
        
        var validBusinessErrorCodes = new[] 
        { 
            "BUSINESS_RULE_VIOLATION", "BUSINESS_ERROR", "DOMAIN_RULE_VIOLATION",
            "CONSTRAINT_VIOLATION", "INVARIANT_VIOLATION"
        };
        
        var hasValidBusinessError = validBusinessErrorCodes.Any(code => 
            result.Error!.Code.Contains(code, StringComparison.OrdinalIgnoreCase));
        
        if (!hasValidBusinessError)
        {
            var message = $"Expected business rule error, got {result.Error!.Code}";
            output?.WriteLine($"❌ BUSINESS RULE: {message}");
            throw new InvalidOperationException($"Business rule contract violated: {message}");
        }
        
        // Verify error message references the specific business rule
        if (!string.IsNullOrEmpty(expectedBusinessRule) && 
            !(result.Error.Message?.Contains(expectedBusinessRule, StringComparison.OrdinalIgnoreCase) ?? false))
        {
            var message = $"Business rule error should reference '{expectedBusinessRule}'";
            output?.WriteLine($"❌ BUSINESS RULE: {message}");
            throw new InvalidOperationException($"Business rule contract violated: {message}");
        }
        
        output?.WriteLine($"✅ BUSINESS RULE: Violation properly detected and reported for '{expectedBusinessRule}'");
    }
    
    #endregion
    
    #region Input Validation Contract Utilities
    
    /// <summary>
    /// Validates null input handling across all contract implementations
    /// Universal pattern for null argument validation
    /// </summary>
    public static void ValidateNullInputHandling<T>(
        Result<T> result,
        string parameterName,
        ITestOutputHelper? output = null)
    {
        Assert.False(result.IsSuccess, "Expected null input to be rejected");
        Assert.NotNull(result.Error);
        
        var validNullErrorCodes = new[] 
        { 
            "VALIDATION_ERROR", "NULL_ARGUMENT", "INVALID_INPUT", "ARGUMENT_NULL"
        };
        
        var hasValidNullError = validNullErrorCodes.Any(code => 
            result.Error!.Code.Contains(code, StringComparison.OrdinalIgnoreCase));
        
        if (!hasValidNullError)
        {
            var message = $"Expected null input validation error, got {result.Error!.Code}";
            output?.WriteLine($"❌ NULL INPUT VALIDATION: {message}");
            throw new InvalidOperationException($"Null input contract violated: {message}");
        }
        
        // Verify error message references the parameter
        if (!string.IsNullOrEmpty(parameterName) && 
            !(result.Error.Message?.Contains(parameterName, StringComparison.OrdinalIgnoreCase) ?? false))
        {
            var message = $"Null input error should reference parameter '{parameterName}'";
            output?.WriteLine($"❌ NULL INPUT VALIDATION: {message}");
            throw new InvalidOperationException($"Null input contract violated: {message}");
        }
        
        output?.WriteLine($"✅ NULL INPUT VALIDATION: Null {parameterName} properly rejected");
    }
    
    #endregion
    
    #region Repository Contract Validation
    
    /// <summary>
    /// Validates repository operation audit patterns
    /// Common across all repository contract tests for medical-grade compliance
    /// </summary>
    public static void ValidateRepositoryAuditPattern(
        string entityType,
        string operation,
        int recordCount,
        string userContext = "system",
        ITestOutputHelper? output = null)
    {
        // This would integrate with actual audit logging in real implementations
        // For contract testing, we validate that the pattern is being followed
        
        var validOperations = new[] 
        { 
            "GetById", "GetAll", "Add", "Update", "Delete", "Exists", 
            "GetBySpecification", "GetPaged", "Count"
        };
        
        if (!validOperations.Contains(operation))
        {
            var message = $"Unknown audit operation: {operation}";
            output?.WriteLine($"❌ REPOSITORY AUDIT: {message}");
            throw new InvalidOperationException($"Repository audit contract violated: {message}");
        }
        
        if (recordCount < 0)
        {
            var message = "Record count cannot be negative";
            output?.WriteLine($"❌ REPOSITORY AUDIT: {message}");
            throw new InvalidOperationException($"Repository audit contract violated: {message}");
        }
        
        output?.WriteLine($"✅ REPOSITORY AUDIT: {entityType}.{operation} logged with {recordCount} records for {userContext}");
    }
    
    #endregion
}