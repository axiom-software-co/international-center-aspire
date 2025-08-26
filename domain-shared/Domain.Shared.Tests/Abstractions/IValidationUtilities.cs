using System.Net;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace InternationalCenter.Shared.Tests.Abstractions;

/// <summary>
/// Contract for test validation utilities with dependency inversion
/// Provides consistent validation patterns across all test domains and types
/// Medical-grade validation ensuring contract compliance and error handling
/// </summary>
public interface IValidationUtilities
{
    /// <summary>
    /// Validates HTTP response security headers for medical-grade compliance
    /// Contract: Must validate all required security headers and log violations
    /// </summary>
    Task ValidateSecurityHeadersAsync(
        HttpResponseMessage response,
        SecurityHeaderRequirements? requirements = null,
        ITestOutputHelper? output = null);
    
    /// <summary>
    /// Validates HTTP response correlation tracking for audit compliance
    /// Contract: Must ensure correlation headers are present for medical-grade audit trails
    /// </summary>
    Task ValidateCorrelationTrackingAsync(
        HttpResponseMessage response,
        CorrelationRequirements? requirements = null,
        ITestOutputHelper? output = null);
    
    /// <summary>
    /// Validates anonymous access patterns for public endpoints
    /// Contract: Must ensure proper anonymous access without authentication requirements
    /// </summary>
    Task ValidateAnonymousAccessAsync(
        HttpResponseMessage response,
        ITestOutputHelper? output = null);
    
    /// <summary>
    /// Validates error response safety ensuring no sensitive information leakage
    /// Contract: Must detect and report any sensitive data in error responses
    /// </summary>
    Task ValidateErrorResponseSafetyAsync<T>(
        IResult<T> result,
        ITestOutputHelper? output = null);
    
    /// <summary>
    /// Validates business rule violation responses
    /// Contract: Must ensure proper business rule error codes and messages
    /// </summary>
    Task ValidateBusinessRuleViolationAsync<T>(
        IResult<T> result,
        string expectedBusinessRule,
        ITestOutputHelper? output = null);
    
    /// <summary>
    /// Validates null input handling across all contract implementations
    /// Contract: Must ensure consistent null input validation and error responses
    /// </summary>
    Task ValidateNullInputHandlingAsync<T>(
        IResult<T> result,
        string parameterName,
        ITestOutputHelper? output = null);
    
    /// <summary>
    /// Validates medical-grade audit context in requests
    /// Contract: Must ensure all required audit properties are present and valid
    /// </summary>
    Task ValidateMedicalGradeAuditContextAsync<TRequest>(
        TRequest request,
        AuditContextRequirements? requirements = null,
        ITestOutputHelper? output = null) where TRequest : class;
    
    /// <summary>
    /// Validates entity integrity against domain rules
    /// Contract: Must validate all domain invariants and business rules
    /// </summary>
    Task ValidateEntityIntegrityAsync<TEntity>(
        TEntity entity,
        EntityValidationRules<TEntity>? rules = null,
        ITestOutputHelper? output = null) where TEntity : class;
    
    /// <summary>
    /// Validates relationship integrity between entities
    /// Contract: Must ensure referential integrity and relationship constraints
    /// </summary>
    Task ValidateRelationshipIntegrityAsync<TParent, TChild>(
        TParent parent,
        TChild child,
        RelationshipValidationRules<TParent, TChild>? rules = null,
        ITestOutputHelper? output = null)
        where TParent : class
        where TChild : class;
    
    /// <summary>
    /// Validates performance contracts for operations
    /// Contract: Must throw PerformanceContractViolationException for violations
    /// </summary>
    Task ValidatePerformanceContractAsync(
        string operationName,
        TimeSpan actualDuration,
        TimeSpan expectedMaxDuration,
        ITestOutputHelper? output = null);
    
    /// <summary>
    /// Validates test data quality and realism
    /// Contract: Must ensure generated test data follows realistic patterns
    /// </summary>
    Task ValidateTestDataQualityAsync<TEntity>(
        TEntity entity,
        TestDataQualityRules<TEntity>? rules = null,
        ITestOutputHelper? output = null) where TEntity : class;
    
    /// <summary>
    /// Validates contract postconditions after operation execution
    /// Contract: Must verify all expected postconditions are met
    /// </summary>
    Task ValidatePostconditionsAsync<TResult>(
        TResult result,
        Func<TResult, bool> postconditionCheck,
        string operationName,
        string postconditionDescription,
        ITestOutputHelper? output = null);
    
    /// <summary>
    /// Validates contract preconditions before operation execution
    /// Contract: Must verify all required preconditions are satisfied
    /// </summary>
    Task ValidatePreconditionsAsync<TInput>(
        TInput input,
        IEnumerable<Func<TInput, bool>> preconditionChecks,
        string operationName,
        ITestOutputHelper? output = null);
}

/// <summary>
/// Configuration for security header validation requirements
/// </summary>
public class SecurityHeaderRequirements
{
    public string[] RequiredHeaders { get; init; } = Array.Empty<string>();
    public bool StrictValidation { get; init; } = true;
    public Dictionary<string, string[]> ExpectedHeaderValues { get; init; } = new();
}

/// <summary>
/// Configuration for correlation tracking validation requirements
/// </summary>
public class CorrelationRequirements
{
    public string[] AcceptableCorrelationHeaders { get; init; } = { "X-Correlation-ID", "X-Request-ID", "TraceId" };
    public bool RequireCorrelationId { get; init; } = true;
    public Func<string, bool>? CorrelationIdValidator { get; init; }
}

/// <summary>
/// Configuration for audit context validation requirements
/// </summary>
public class AuditContextRequirements
{
    public string[] RequiredAuditProperties { get; init; } = Array.Empty<string>();
    public bool RequireUserContext { get; init; } = true;
    public bool RequireTimestamp { get; init; } = true;
    public bool RequireCorrelationId { get; init; } = true;
}

/// <summary>
/// Validation rules for entity integrity checking
/// </summary>
public class EntityValidationRules<TEntity> where TEntity : class
{
    public Func<TEntity, bool>[] Invariants { get; init; } = Array.Empty<Func<TEntity, bool>>();
    public Func<TEntity, Task<bool>>[] AsyncValidators { get; init; } = Array.Empty<Func<TEntity, Task<bool>>>();
    public Dictionary<string, Func<TEntity, bool>> PropertyValidators { get; init; } = new();
}

/// <summary>
/// Validation rules for relationship integrity checking
/// </summary>
public class RelationshipValidationRules<TParent, TChild>
    where TParent : class
    where TChild : class
{
    public Func<TParent, TChild, bool>[] RelationshipInvariants { get; init; } = Array.Empty<Func<TParent, TChild, bool>>();
    public Func<TParent, TChild, Task<bool>>[] AsyncRelationshipValidators { get; init; } = Array.Empty<Func<TParent, TChild, Task<bool>>>();
}

/// <summary>
/// Quality rules for test data validation
/// </summary>
public class TestDataQualityRules<TEntity> where TEntity : class
{
    public Func<TEntity, bool> IsRealistic { get; init; } = _ => true;
    public Func<TEntity, bool> FollowsDomainRules { get; init; } = _ => true;
    public Func<TEntity, bool> HasRequiredProperties { get; init; } = _ => true;
    public string[] ForbiddenTestPatterns { get; init; } = { "Test", "Mock", "Fake", "Lorem ipsum" };
}

/// <summary>
/// Generic result interface for validation utilities
/// </summary>
public interface IResult<T>
{
    bool IsSuccess { get; }
    T? Value { get; }
    IError? Error { get; }
}

/// <summary>
/// Generic error interface for validation utilities
/// </summary>
public interface IError
{
    string Code { get; }
    string Message { get; }
    string? Details { get; }
}

/// <summary>
/// Exception thrown when performance contracts are violated
/// </summary>
public class PerformanceContractViolationException : Exception
{
    public string OperationName { get; }
    public TimeSpan ActualDuration { get; }
    public TimeSpan ExpectedMaxDuration { get; }
    public double ViolationPercentage { get; }

    public PerformanceContractViolationException(
        string operationName,
        TimeSpan actualDuration,
        TimeSpan expectedMaxDuration)
        : base($"Performance contract violation: {operationName} took {actualDuration.TotalMilliseconds:F0}ms, expected max {expectedMaxDuration.TotalMilliseconds:F0}ms")
    {
        OperationName = operationName;
        ActualDuration = actualDuration;
        ExpectedMaxDuration = expectedMaxDuration;
        ViolationPercentage = (actualDuration.TotalMilliseconds - expectedMaxDuration.TotalMilliseconds) / expectedMaxDuration.TotalMilliseconds * 100;
    }
}

/// <summary>
/// Exception thrown when validation contracts are violated
/// </summary>
public class ValidationContractViolationException : Exception
{
    public string ValidationName { get; }
    public string ExpectedBehavior { get; }
    public string ActualBehavior { get; }

    public ValidationContractViolationException(
        string validationName,
        string expectedBehavior,
        string actualBehavior)
        : base($"Validation contract violation: {validationName} - Expected: {expectedBehavior}, Actual: {actualBehavior}")
    {
        ValidationName = validationName;
        ExpectedBehavior = expectedBehavior;
        ActualBehavior = actualBehavior;
    }
}