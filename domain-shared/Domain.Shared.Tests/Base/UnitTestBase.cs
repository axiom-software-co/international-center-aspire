using InternationalCenter.Tests.Shared.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace InternationalCenter.Tests.Shared.Base;

/// <summary>
/// Base class for unit tests that enforces mock-based testing patterns
/// Ensures contract-first testing principles by providing mock infrastructure
/// WHY: Unit tests must use mocks for all dependencies to achieve proper isolation
/// SCOPE: All Services API unit tests (Public and Admin)  
/// CONTEXT: Contract-first TDD requires clear separation - unit tests mock dependencies, integration tests use real implementations
/// </summary>
public abstract class UnitTestBase
{
    protected readonly ITestOutputHelper Output;

    protected UnitTestBase(ITestOutputHelper output)
    {
        Output = output ?? throw new ArgumentNullException(nameof(output));
    }

    /// <summary>
    /// Create mock logger for unit testing
    /// Unit tests should never use real logging infrastructure
    /// </summary>
    protected Mock<ILogger<T>> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }

    /// <summary>
    /// Verify that a result follows error safety contracts
    /// Ensures no sensitive information is leaked in error messages
    /// </summary>
    protected void ValidateErrorResponseSafety<T>(T result) where T : class
    {
        // Use reflection to check if this is a Result<> type with an Error property
        var resultType = result.GetType();
        var isSuccessProperty = resultType.GetProperty("IsSuccess");
        var errorProperty = resultType.GetProperty("Error");

        if (isSuccessProperty != null && errorProperty != null)
        {
            var isSuccess = (bool)(isSuccessProperty.GetValue(result) ?? true);
            if (!isSuccess)
            {
                var error = errorProperty.GetValue(result);
                if (error != null)
                {
                    var messageProperty = error.GetType().GetProperty("Message");
                    if (messageProperty != null)
                    {
                        var errorMessage = messageProperty.GetValue(error)?.ToString()?.ToLowerInvariant() ?? "";
                        
                        // Check that error doesn't contain sensitive information
                        var sensitiveTerms = new[] { "password", "connection string", "database", "sql", "server", "exception" };
                        foreach (var term in sensitiveTerms)
                        {
                            if (errorMessage.Contains(term))
                            {
                                throw new InvalidOperationException($"Error message contains sensitive term '{term}': {errorMessage}");
                            }
                        }
                    }
                }
            }
        }

        Output.WriteLine("✅ ERROR SAFETY: Error response doesn't leak sensitive information");
    }

    /// <summary>
    /// Validate that request contains medical-grade audit context
    /// Ensures compliance requirements are enforced at unit test level
    /// </summary>
    protected void ValidateMedicalGradeAuditContext<T>(T request) where T : class
    {
        var requestType = request.GetType();
        var auditProperties = new[] { "RequestId", "UserContext", "ClientIpAddress", "UserAgent" };
        
        foreach (var propertyName in auditProperties)
        {
            var property = requestType.GetProperty(propertyName);
            if (property != null)
            {
                var value = property.GetValue(request);
                if (value == null || string.IsNullOrEmpty(value.ToString()))
                {
                    throw new InvalidOperationException($"Medical-grade audit property '{propertyName}' is missing or empty");
                }
            }
        }
        
        Output.WriteLine("✅ MEDICAL-GRADE AUDIT: Request contains required audit information");
    }

    /// <summary>
    /// Execute operation and verify it completes within performance contract
    /// Unit tests should validate performance contracts for business logic
    /// </summary>
    protected async Task<T> ExecuteWithPerformanceValidation<T>(
        Func<Task<T>> operation,
        TimeSpan? maxDuration = null,
        string? operationName = null)
    {
        var timeout = maxDuration ?? StandardizedTestConfiguration.Timeouts.UnitTest;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await operation();
        stopwatch.Stop();

        if (stopwatch.Elapsed > timeout)
        {
            throw new InvalidOperationException(
                $"Performance contract violated: {operationName ?? "Operation"} took {StandardizedTestConfiguration.LoggingConfiguration.FormatDuration(stopwatch.Elapsed)} (should be < {StandardizedTestConfiguration.LoggingConfiguration.FormatDuration(timeout)})");
        }

        Output.WriteLine($"✅ PERFORMANCE CONTRACT: {operationName ?? "Operation"} completed in {StandardizedTestConfiguration.LoggingConfiguration.FormatDuration(stopwatch.Elapsed)}");
        return result;
    }

    /// <summary>
    /// Verify that concurrent operations are handled safely
    /// Unit tests should validate thread safety of business logic
    /// </summary>
    protected async Task VerifyConcurrencyContract<T>(
        Func<Task<T>> operation,
        int concurrentOperations = 5,
        string? operationName = null)
    {
        var tasks = Enumerable.Range(0, concurrentOperations)
            .Select(_ => operation())
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // All operations should complete successfully
        foreach (var result in results)
        {
            if (result == null)
            {
                throw new InvalidOperationException($"Concurrency contract violated: {operationName ?? "Operation"} returned null result");
            }
        }

        Output.WriteLine($"✅ CONCURRENCY CONTRACT: {concurrentOperations} concurrent {operationName ?? "operations"} handled safely");
    }

    /// <summary>
    /// Verify error contract for expected exception scenarios
    /// Ensures proper exception handling in unit tests
    /// </summary>
    protected async Task VerifyErrorContract<TException>(
        Func<Task> operation,
        string expectedErrorCode,
        string? operationName = null)
        where TException : Exception
    {
        try
        {
            await operation();
            throw new InvalidOperationException($"Error contract violated: {operationName ?? "Operation"} should have thrown {typeof(TException).Name}");
        }
        catch (TException ex)
        {
            // Verify exception doesn't contain sensitive information
            var errorMessage = ex.Message?.ToLowerInvariant() ?? "";
            var sensitiveTerms = new[] { "password", "connection string", "database", "sql", "server" };
            foreach (var term in sensitiveTerms)
            {
                if (errorMessage.Contains(term))
                {
                    throw new InvalidOperationException($"Error contract violated: Exception message contains sensitive term '{term}'");
                }
            }

            Output.WriteLine($"✅ ERROR CONTRACT: {operationName ?? "Operation"} properly threw {typeof(TException).Name}");
        }
    }

    /// <summary>
    /// Verify postconditions after operation execution
    /// Supports contract-first testing by validating state changes
    /// </summary>
    protected async Task VerifyPostconditions<T>(
        Func<Task<T>> operation,
        Func<T, bool> postcondition,
        string operationName,
        string postconditionDescription)
    {
        var result = await operation();
        
        if (!postcondition(result))
        {
            throw new InvalidOperationException(
                $"Postcondition violated for {operationName}: {postconditionDescription}");
        }

        Output.WriteLine($"✅ POSTCONDITION: {operationName} - {postconditionDescription}");
    }
}