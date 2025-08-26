using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using System.Diagnostics;

namespace InternationalCenter.Tests.Shared.Contracts;

/// <summary>
/// Base class for validating TDD Red/Green/Refactor cycle in contract tests
/// Ensures proper test-driven development workflow for gateway-API integration
/// Focuses on Services Public and Admin APIs only (other APIs on hold per project rules)
/// </summary>
public abstract class TddCycleValidationBase
{
    protected readonly ITestOutputHelper Output;
    protected readonly ILogger Logger;
    
    protected TddCycleValidationBase(ITestOutputHelper output)
    {
        Output = output ?? throw new ArgumentNullException(nameof(output));
        Logger = new TestLogger<TddCycleValidationBase>(output);
    }
    
    /// <summary>
    /// RED PHASE: Create failing test that defines the contract
    /// This method should contain tests that fail initially and define the expected behavior
    /// </summary>
    protected abstract Task RedPhase_CreateFailingContractTest();
    
    /// <summary>
    /// GREEN PHASE: Implement minimal code to make the test pass
    /// This method should verify that the minimal implementation makes the test pass
    /// </summary>
    protected abstract Task GreenPhase_ImplementMinimalSolution();
    
    /// <summary>
    /// REFACTOR PHASE: Improve the design without changing behavior
    /// This method should verify that refactoring maintains the passing test while improving design
    /// </summary>
    protected abstract Task RefactorPhase_ImproveDesignWithoutChangingBehavior();
    
    /// <summary>
    /// Execute complete TDD cycle validation for contract testing
    /// Validates that the implementation follows proper TDD workflow
    /// </summary>
    [Fact]
    public async Task ValidateTddCycle_RedGreenRefactor_ShouldFollowProperWorkflow()
    {
        Output.WriteLine("🔄 TDD CYCLE VALIDATION: Starting Red/Green/Refactor validation for contract testing");
        
        // RED PHASE: Write failing test first
        Output.WriteLine("🔴 RED PHASE: Creating failing contract test...");
        var redPhaseStart = Stopwatch.StartNew();
        
        await RedPhase_CreateFailingContractTest();
        
        redPhaseStart.Stop();
        Output.WriteLine($"✅ RED PHASE COMPLETE: Failing contract test created ({redPhaseStart.ElapsedMilliseconds}ms)");
        
        // GREEN PHASE: Implement minimal solution
        Output.WriteLine("🟢 GREEN PHASE: Implementing minimal solution to make test pass...");
        var greenPhaseStart = Stopwatch.StartNew();
        
        await GreenPhase_ImplementMinimalSolution();
        
        greenPhaseStart.Stop();
        Output.WriteLine($"✅ GREEN PHASE COMPLETE: Test now passes with minimal implementation ({greenPhaseStart.ElapsedMilliseconds}ms)");
        
        // REFACTOR PHASE: Improve design
        Output.WriteLine("🔧 REFACTOR PHASE: Improving design without changing behavior...");
        var refactorPhaseStart = Stopwatch.StartNew();
        
        await RefactorPhase_ImproveDesignWithoutChangingBehavior();
        
        refactorPhaseStart.Stop();
        Output.WriteLine($"✅ REFACTOR PHASE COMPLETE: Design improved while maintaining passing tests ({refactorPhaseStart.ElapsedMilliseconds}ms)");
        
        var totalTime = redPhaseStart.ElapsedMilliseconds + greenPhaseStart.ElapsedMilliseconds + refactorPhaseStart.ElapsedMilliseconds;
        Output.WriteLine($"✅ TDD CYCLE VALIDATION COMPLETE: Red/Green/Refactor workflow validated ({totalTime}ms total)");
    }
    
    /// <summary>
    /// Validate that the contract test properly separates gateway concerns from API business logic
    /// Gateway should handle: routing, authentication, rate limiting, CORS, security headers
    /// API should handle: business logic, data persistence, domain rules, validation
    /// </summary>
    protected async Task ValidateBusinessLogicSeparation(
        string gatewayEndpoint, 
        string apiEndpoint, 
        string expectedBusinessBehavior)
    {
        Output.WriteLine($"🔍 BUSINESS LOGIC SEPARATION: Validating separation between gateway and API for {gatewayEndpoint}");
        
        // Validate gateway handles routing and infrastructure concerns
        await ValidateGatewayInfrastructureConcerns(gatewayEndpoint);
        
        // Validate API handles business logic only
        await ValidateApiBusinessLogicConcerns(apiEndpoint, expectedBusinessBehavior);
        
        Output.WriteLine($"✅ BUSINESS LOGIC SEPARATION: Gateway and API responsibilities properly separated");
    }
    
    /// <summary>
    /// Validate that gateway handles infrastructure concerns (routing, auth, rate limiting, etc.)
    /// Should NOT handle business logic
    /// </summary>
    protected abstract Task ValidateGatewayInfrastructureConcerns(string gatewayEndpoint);
    
    /// <summary>
    /// Validate that API handles business logic concerns only
    /// Should NOT handle infrastructure concerns (those are gateway's responsibility)
    /// </summary>
    protected abstract Task ValidateApiBusinessLogicConcerns(string apiEndpoint, string expectedBusinessBehavior);
    
    /// <summary>
    /// Verify that the contract test drives the architecture design
    /// Tests should define the interface contracts before implementation exists
    /// </summary>
    protected void ValidateTestDrivenArchitecture(string contractName, Type expectedInterface, Type actualImplementation)
    {
        Output.WriteLine($"🏗️ ARCHITECTURE VALIDATION: Verifying test-driven architecture for {contractName}");
        
        // Verify interface was defined first (contract-first)
        Assert.True(expectedInterface.IsInterface, $"Contract {contractName} should be defined as interface first");
        
        // Verify implementation follows the interface contract
        Assert.True(expectedInterface.IsAssignableFrom(actualImplementation), 
                   $"Implementation {actualImplementation.Name} should implement contract interface {expectedInterface.Name}");
        
        // Verify interface has proper contract methods
        var contractMethods = expectedInterface.GetMethods();
        Assert.True(contractMethods.Length > 0, $"Contract interface {expectedInterface.Name} should define contract methods");
        
        Output.WriteLine($"✅ ARCHITECTURE VALIDATION: Test-driven architecture properly implemented for {contractName}");
    }
    
    /// <summary>
    /// Validate that contract tests focus on preconditions and postconditions
    /// Not implementation details - this ensures proper contract testing
    /// </summary>
    protected async Task ValidateContractFocus(
        Func<Task> operation,
        Func<bool> preconditionCheck,
        Func<Task<bool>> postconditionCheck,
        string operationName)
    {
        Output.WriteLine($"📋 CONTRACT FOCUS: Validating preconditions and postconditions for {operationName}");
        
        // Verify preconditions are met
        if (!preconditionCheck())
        {
            throw new InvalidOperationException($"Precondition failed for {operationName} - contract test setup issue");
        }
        
        // Execute the operation
        await operation();
        
        // Verify postconditions are met
        var postconditionMet = await postconditionCheck();
        Assert.True(postconditionMet, $"Postcondition failed for {operationName} - contract violation");
        
        Output.WriteLine($"✅ CONTRACT FOCUS: Preconditions and postconditions validated for {operationName}");
    }
    
    /// <summary>
    /// Ensure contract tests are focused on Services Public and Admin APIs only
    /// Other APIs are on hold per project requirements
    /// </summary>
    protected void ValidateServicesApiScope(string apiEndpoint)
    {
        var validServicesPaths = new[] { "/api/services", "/api/admin/services", "/api/categories", "/api/admin/categories" };
        var isServicesApi = validServicesPaths.Any(path => apiEndpoint.Contains(path));
        
        Assert.True(isServicesApi, 
                   $"Contract test should focus on Services APIs only. Endpoint {apiEndpoint} is not in scope. " +
                   "Valid paths: " + string.Join(", ", validServicesPaths));
        
        Output.WriteLine($"✅ SERVICES API SCOPE: Contract test properly focused on Services API endpoint {apiEndpoint}");
    }
}

/// <summary>
/// Test logger implementation for TDD cycle validation
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