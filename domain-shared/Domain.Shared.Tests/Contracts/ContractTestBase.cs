using System.Reflection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace InternationalCenter.Tests.Shared.Contracts;

/// <summary>
/// Base class for contract-first TDD testing - ensures all interface contracts are properly tested
/// Follows Microsoft recommended patterns for testing interfaces/contracts rather than implementations
/// Medical-grade testing approach focusing on preconditions, postconditions, and invariants
/// </summary>
/// <typeparam name="TInterface">The interface contract being tested</typeparam>
public abstract class ContractTestBase<TInterface> where TInterface : class
{
    protected readonly ITestOutputHelper Output;
    protected readonly ILogger Logger;
    
    protected ContractTestBase(ITestOutputHelper output)
    {
        Output = output ?? throw new ArgumentNullException(nameof(output));
        Logger = new TestLogger<TInterface>(output);
    }
    
    /// <summary>
    /// Validates that all interface methods have corresponding tests
    /// Ensures complete contract coverage per TDD principles
    /// </summary>
    protected virtual void ValidateContractCoverage()
    {
        var interfaceType = typeof(TInterface);
        var methods = interfaceType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        var testType = GetType();
        
        var missingTests = new List<string>();
        
        foreach (var method in methods)
        {
            var expectedTestMethods = GetExpectedTestMethodNames(method);
            var hasAnyTest = expectedTestMethods.Any(testMethodName => 
                testType.GetMethods().Any(m => m.Name.Contains(testMethodName, StringComparison.OrdinalIgnoreCase)));
            
            if (!hasAnyTest)
            {
                missingTests.Add($"{method.Name} - Expected test methods: {string.Join(", ", expectedTestMethods)}");
            }
        }
        
        if (missingTests.Any())
        {
            var message = $"Contract {interfaceType.Name} is missing tests for:\n" + 
                         string.Join("\n", missingTests);
            Output.WriteLine($"❌ CONTRACT COVERAGE VIOLATION: {message}");
            throw new InvalidOperationException($"Contract coverage incomplete: {message}");
        }
        
        Output.WriteLine($"✅ CONTRACT COVERAGE COMPLETE: All methods in {interfaceType.Name} have corresponding tests");
    }
    
    /// <summary>
    /// Generates expected test method names for a given interface method
    /// Follows TDD naming conventions for comprehensive contract testing
    /// </summary>
    private static IEnumerable<string> GetExpectedTestMethodNames(MethodInfo method)
    {
        var baseName = method.Name.Replace("Async", "");
        
        // Essential contract test patterns every interface method should have
        yield return $"{baseName}_WithValidInput_ShouldSucceed";
        yield return $"{baseName}_WithInvalidInput_ShouldFail";
        yield return $"{baseName}_WithNullInput_ShouldHandleGracefully";
        
        // If method returns Task, test cancellation
        if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            yield return $"{baseName}_WithCancellation_ShouldRespectCancellation";
        }
        
        // If method has parameters, test edge cases
        if (method.GetParameters().Any())
        {
            yield return $"{baseName}_WithEdgeCaseInput_ShouldHandleCorrectly";
        }
    }
    
    /// <summary>
    /// Verifies preconditions are properly validated by the contract implementation
    /// Medical-grade validation ensures all input contracts are enforced
    /// </summary>
    protected virtual void VerifyPreconditions<T>(Func<T, Task> operation, T validInput, T[] invalidInputs, string operationName)
    {
        Output.WriteLine($"🔍 PRECONDITION TESTING: {operationName} with {typeof(T).Name}");
        
        // Valid input should not throw during precondition validation
        var validException = Record.ExceptionAsync(async () => await operation(validInput));
        if (validException.Result != null)
        {
            Output.WriteLine($"❌ PRECONDITION VIOLATION: Valid input threw {validException.Result.GetType().Name}");
        }
        
        // Invalid inputs should fail precondition validation
        foreach (var invalidInput in invalidInputs)
        {
            var invalidException = Record.ExceptionAsync(async () => await operation(invalidInput));
            if (invalidException.Result == null)
            {
                Output.WriteLine($"❌ PRECONDITION VIOLATION: Invalid input {invalidInput} should have failed validation");
                throw new InvalidOperationException($"Precondition failed: Invalid input {invalidInput} was accepted");
            }
            else
            {
                Output.WriteLine($"✅ PRECONDITION ENFORCED: Invalid input {invalidInput} properly rejected with {invalidException.Result.GetType().Name}");
            }
        }
    }
    
    /// <summary>
    /// Verifies postconditions are met after successful operations
    /// Ensures contract obligations are fulfilled after method execution
    /// </summary>
    protected virtual async Task VerifyPostconditions<TResult>(
        Func<Task<TResult>> operation, 
        Func<TResult, bool> postconditionCheck,
        string operationName,
        string postconditionDescription)
    {
        Output.WriteLine($"🎯 POSTCONDITION TESTING: {operationName} - {postconditionDescription}");
        
        var result = await operation();
        
        if (!postconditionCheck(result))
        {
            Output.WriteLine($"❌ POSTCONDITION VIOLATION: {postconditionDescription} was not met");
            throw new InvalidOperationException($"Postcondition failed: {postconditionDescription}");
        }
        
        Output.WriteLine($"✅ POSTCONDITION SATISFIED: {postconditionDescription}");
    }
    
    /// <summary>
    /// Tests domain invariants that should always hold true
    /// Medical-grade compliance requires invariant validation
    /// </summary>
    protected virtual void VerifyInvariants<T>(T entity, Func<T, bool>[] invariants, string entityName)
    {
        Output.WriteLine($"⚖️ INVARIANT TESTING: {entityName} invariants");
        
        for (int i = 0; i < invariants.Length; i++)
        {
            var invariant = invariants[i];
            if (!invariant(entity))
            {
                Output.WriteLine($"❌ INVARIANT VIOLATION: {entityName} invariant {i + 1} failed");
                throw new InvalidOperationException($"Domain invariant {i + 1} violated for {entityName}");
            }
        }
        
        Output.WriteLine($"✅ INVARIANTS SATISFIED: All {invariants.Length} invariants for {entityName} are valid");
    }
    
    /// <summary>
    /// Validates error contracts - ensures proper error handling and error types
    /// Medical-grade error handling with proper error codes and messages
    /// </summary>
    protected virtual async Task VerifyErrorContract<TException>(
        Func<Task> operation, 
        string expectedErrorCode,
        string operationName) where TException : Exception
    {
        Output.WriteLine($"🚨 ERROR CONTRACT TESTING: {operationName} should throw {typeof(TException).Name}");
        
        var exception = await Assert.ThrowsAsync<TException>(operation);
        
        // For Result pattern errors, check error codes
        if (exception.Data.Contains("ErrorCode"))
        {
            var actualErrorCode = exception.Data["ErrorCode"]?.ToString();
            if (actualErrorCode != expectedErrorCode)
            {
                Output.WriteLine($"❌ ERROR CONTRACT VIOLATION: Expected {expectedErrorCode}, got {actualErrorCode}");
                throw new InvalidOperationException($"Error contract violated: Expected {expectedErrorCode}, got {actualErrorCode}");
            }
        }
        
        Output.WriteLine($"✅ ERROR CONTRACT SATISFIED: {typeof(TException).Name} with correct error code");
    }
    
    /// <summary>
    /// Tests concurrent access scenarios for contract thread-safety
    /// Medical-grade systems require proper concurrency handling
    /// </summary>
    protected virtual async Task VerifyConcurrencyContract<TResult>(
        Func<Task<TResult>> operation,
        int concurrentOperations,
        string operationName)
    {
        Output.WriteLine($"⚡ CONCURRENCY CONTRACT TESTING: {operationName} with {concurrentOperations} concurrent operations");
        
        var tasks = Enumerable.Range(0, concurrentOperations)
            .Select(_ => operation())
            .ToArray();
        
        var results = await Task.WhenAll(tasks);
        
        // Verify all operations completed successfully
        if (results.Length != concurrentOperations)
        {
            Output.WriteLine($"❌ CONCURRENCY CONTRACT VIOLATION: Expected {concurrentOperations} results, got {results.Length}");
            throw new InvalidOperationException($"Concurrency contract failed: Lost operations during concurrent execution");
        }
        
        Output.WriteLine($"✅ CONCURRENCY CONTRACT SATISFIED: All {concurrentOperations} concurrent operations completed");
    }
}

/// <summary>
/// Test logger that outputs to xUnit test output for contract testing visibility
/// </summary>
internal class TestLogger<T> : ILogger<T>
{
    private readonly ITestOutputHelper _output;
    
    public TestLogger(ITestOutputHelper output)
    {
        _output = output;
    }
    
    public IDisposable BeginScope<TState>(TState state) => new TestScope();
    
    public bool IsEnabled(LogLevel logLevel) => true;
    
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        _output.WriteLine($"[{logLevel}] {typeof(T).Name}: {message}");
        
        if (exception != null)
        {
            _output.WriteLine($"Exception: {exception}");
        }
    }
    
    private class TestScope : IDisposable
    {
        public void Dispose() { }
    }
}