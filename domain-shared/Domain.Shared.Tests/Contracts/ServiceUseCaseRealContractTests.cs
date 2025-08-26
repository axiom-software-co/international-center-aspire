using InternationalCenter.Services.Domain.Models;
using InternationalCenter.Services.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Xunit.Abstractions;
using Aspire.Hosting.Testing;

namespace InternationalCenter.Tests.Shared.Contracts;

/// <summary>
/// Contract tests for Service Use Cases using real implementations - no mocks
/// Medical-grade testing focusing on business rule enforcement and error handling contracts
/// Validates preconditions, postconditions, and audit compliance for Services APIs
/// Uses DistributedApplicationTestingBuilder for proper gateway-API integration testing
/// </summary>
/// <typeparam name="TUseCase">The use case implementation type</typeparam>
/// <typeparam name="TRequest">The use case request type</typeparam>
/// <typeparam name="TResponse">The use case response type</typeparam>
public abstract class ServiceUseCaseRealContractTests<TUseCase, TRequest, TResponse> : 
    ContractTestBase<TUseCase>, IUseCaseContract<TRequest, TResponse>
    where TUseCase : class
    where TRequest : class
    where TResponse : class
{
    protected IServiceScope ServiceScope { get; private set; }
    protected TUseCase UseCase { get; private set; }
    protected IServiceRepository ServiceRepository { get; private set; }
    protected IServiceCategoryRepository CategoryRepository { get; private set; }
    protected ILogger<TUseCase> UseCaseLogger { get; private set; }
    
    protected ServiceUseCaseRealContractTests(ITestOutputHelper output) : base(output)
    {
        InitializeRealImplementations();
    }
    
    /// <summary>
    /// Initializes real service implementations for contract testing
    /// No mocks - uses actual dependency injection container and real implementations
    /// </summary>
    private void InitializeRealImplementations()
    {
        var services = new ServiceCollection();
        
        // Configure real services for contract testing
        ConfigureRealServices(services);
        
        var serviceProvider = services.BuildServiceProvider();
        ServiceScope = serviceProvider.CreateScope();
        
        // Get real implementations from DI container
        UseCase = ServiceScope.ServiceProvider.GetRequiredService<TUseCase>();
        ServiceRepository = ServiceScope.ServiceProvider.GetRequiredService<IServiceRepository>();
        CategoryRepository = ServiceScope.ServiceProvider.GetRequiredService<IServiceCategoryRepository>();
        UseCaseLogger = ServiceScope.ServiceProvider.GetRequiredService<ILogger<TUseCase>>();
        
        Output.WriteLine("✅ REAL IMPLEMENTATIONS: Contract test initialized with real services (no mocks)");
    }
    
    /// <summary>
    /// Configure real services for contract testing
    /// Override this method to register your specific use case and dependencies
    /// </summary>
    protected abstract void ConfigureRealServices(IServiceCollection services);
    
    /// <summary>
    /// Factory method for creating valid request for positive test scenarios
    /// </summary>
    protected abstract TRequest CreateValidRequest();
    
    /// <summary>
    /// Factory method for creating invalid requests for negative test scenarios
    /// </summary>
    protected abstract TRequest[] CreateInvalidRequests();
    
    /// <summary>
    /// Abstract method to execute the use case with real implementation
    /// </summary>
    protected abstract Task<Result<TResponse>> ExecuteUseCaseAsync(TRequest request, CancellationToken cancellationToken = default);
    
    #region Contract Implementation - Input Validation with Real Services
    
    [Fact]
    public virtual async Task VerifyExecuteAsync_WithNullRequest_ReturnsValidationError()
    {
        // Arrange
        TRequest? nullRequest = null;
        
        // Act - Using real implementation, not mocks
        var result = await ExecuteUseCaseAsync(nullRequest!);
        
        // Assert - Verify validation error contract with real implementation
        await VerifyPostconditions(
            async () => result,
            r => !r.IsSuccess && (r.Error?.Code == "VALIDATION_ERROR" || r.Error?.Code.Contains("VALIDATION")),
            "ExecuteAsync with null request (real implementation)",
            "Should return validation error for null request using real validator");
        
        // Verify error message contains helpful information
        Assert.Contains("request", result.Error?.Message?.ToLowerInvariant() ?? "");
        
        Output.WriteLine("✅ REAL VALIDATION CONTRACT: Null request properly rejected by real validator");
    }
    
    [Fact]
    public virtual async Task VerifyExecuteAsync_WithInvalidRequests_ReturnsValidationErrors()
    {
        // Arrange
        var invalidRequests = CreateInvalidRequests();
        
        foreach (var invalidRequest in invalidRequests)
        {
            // Act - Using real validation logic
            var result = await ExecuteUseCaseAsync(invalidRequest);
            
            // Assert - Each invalid request should fail real validation
            await VerifyPostconditions(
                async () => result,
                r => !r.IsSuccess && (
                    r.Error?.Code == "VALIDATION_ERROR" || 
                    r.Error?.Code == "BUSINESS_RULE_VIOLATION" ||
                    r.Error?.Code.Contains("VALIDATION") ||
                    r.Error?.Code.Contains("BUSINESS")),
                $"ExecuteAsync with invalid request {invalidRequest.GetType().Name} (real implementation)",
                "Should return validation or business rule error using real business logic");
            
            Output.WriteLine($"✅ REAL VALIDATION CONTRACT: Invalid request {invalidRequest.GetType().Name} properly rejected by real validator");
        }
    }
    
    #endregion
    
    #region Contract Implementation - Business Rules with Real Implementation
    
    [Fact]
    public virtual async Task VerifyExecuteAsync_WithBusinessRuleViolation_ReturnsBusinessError()
    {
        // Using real business rule enforcement instead of mocked scenarios
        await VerifyRealBusinessRuleEnforcement();
        Output.WriteLine("✅ REAL BUSINESS RULE CONTRACT: Business rules properly enforced by real implementation");
    }
    
    /// <summary>
    /// Test real business rule enforcement using actual business logic
    /// No mocks - tests against actual business rule implementations
    /// </summary>
    protected abstract Task VerifyRealBusinessRuleEnforcement();
    
    #endregion
    
    #region Contract Implementation - Infrastructure Resilience with Real Repositories
    
    [Fact]
    public virtual async Task VerifyExecuteAsync_WithDatabaseConstraintViolation_ReturnsInfrastructureError()
    {
        // Arrange - Create request that will violate real database constraints
        var constraintViolatingRequest = CreateDatabaseConstraintViolatingRequest();
        
        // Act - Execute against real database
        var result = await ExecuteUseCaseAsync(constraintViolatingRequest);
        
        // Assert - Verify real infrastructure error handling
        await VerifyPostconditions(
            async () => result,
            r => !r.IsSuccess && (
                r.Error?.Code == "REPOSITORY_ERROR" || 
                r.Error?.Code == "INFRASTRUCTURE_ERROR" ||
                r.Error?.Code == "DATABASE_ERROR" ||
                r.Error?.Code.Contains("CONSTRAINT")),
            "ExecuteAsync with constraint violation (real database)",
            "Should return infrastructure error from real database constraint violation");
        
        // Verify error message doesn't expose internal implementation details
        var errorMessage = result.Error?.Message?.ToLowerInvariant() ?? "";
        Assert.DoesNotContain("constraint", errorMessage);
        Assert.DoesNotContain("foreign key", errorMessage);
        Assert.DoesNotContain("unique", errorMessage);
        
        Output.WriteLine("✅ REAL INFRASTRUCTURE CONTRACT: Database constraint violations handled gracefully");
    }
    
    /// <summary>
    /// Create request that will violate actual database constraints for testing
    /// Override to provide constraint-violating scenarios for your use case
    /// </summary>
    protected abstract TRequest CreateDatabaseConstraintViolatingRequest();
    
    #endregion
    
    #region Contract Implementation - Success Scenarios with Real Data
    
    [Fact]
    public virtual async Task VerifyExecuteAsync_WithValidRequest_ReturnsSuccessResult()
    {
        // Arrange - Setup real test data in database
        await SetupRealTestData();
        var validRequest = CreateValidRequest();
        
        // Act - Execute with real implementation
        var result = await ExecuteUseCaseAsync(validRequest);
        
        // Assert - Verify success contract with real result
        await VerifyPostconditions(
            async () => result,
            r => r.IsSuccess && r.Value != null,
            "ExecuteAsync with valid request (real implementation)",
            "Should return success result with real response data");
        
        // Cleanup real test data
        await CleanupRealTestData();
        
        Output.WriteLine("✅ REAL SUCCESS CONTRACT: Valid request processed successfully with real implementation");
    }
    
    /// <summary>
    /// Setup real test data in database for successful test scenarios
    /// Override to create necessary test data for your use case
    /// </summary>
    protected abstract Task SetupRealTestData();
    
    /// <summary>
    /// Cleanup real test data after test completion
    /// Override to clean up test data specific to your use case
    /// </summary>
    protected abstract Task CleanupRealTestData();
    
    #endregion
    
    #region Contract Implementation - Audit and Compliance with Real Logging
    
    [Fact]
    public virtual async Task VerifyExecuteAsync_WithAnyRequest_LogsRealMedicalGradeAuditTrail()
    {
        // Arrange - Setup real test data
        await SetupRealTestData();
        var validRequest = CreateValidRequest();
        
        // Capture real logging output
        var logCapture = new List<string>();
        
        // Act - Execute with real logging infrastructure
        var result = await ExecuteUseCaseAsync(validRequest);
        
        // Assert - Verify real audit logging occurred
        Assert.NotNull(result);
        
        // Verify structured logging was used (not string concatenation)
        // Real implementation should use structured logging with correlation IDs
        await VerifyRealAuditLogging(validRequest, result);
        
        // Cleanup
        await CleanupRealTestData();
        
        Output.WriteLine("✅ REAL AUDIT CONTRACT: Use case execution logged by real medical-grade audit system");
    }
    
    /// <summary>
    /// Verify real audit logging implementation
    /// Override to validate audit logging specific to your use case
    /// </summary>
    protected abstract Task VerifyRealAuditLogging(TRequest request, Result<TResponse> result);
    
    #endregion
    
    #region Contract Implementation - Concurrency with Real Database
    
    [Fact]
    public virtual async Task VerifyExecuteAsync_WithConcurrentRequests_HandlesAllRequestsSafelyWithRealDb()
    {
        // Arrange - Setup real test data for concurrent operations
        await SetupRealTestData();
        var requests = Enumerable.Range(0, 5).Select(_ => CreateValidConcurrentRequest()).ToArray();
        
        // Act & Assert - Test real concurrency handling
        await VerifyConcurrencyContract(
            async () => await ExecuteUseCaseAsync(requests[0]),
            concurrentOperations: 5,
            "Concurrent use case execution with real database");
        
        // Verify database consistency after concurrent operations
        await VerifyDatabaseConsistencyAfterConcurrentOperations();
        
        // Cleanup
        await CleanupRealTestData();
        
        Output.WriteLine("✅ REAL CONCURRENCY CONTRACT: Concurrent requests handled safely with real database");
    }
    
    /// <summary>
    /// Create valid request for concurrent execution testing
    /// Override to provide concurrent-safe request for your use case
    /// </summary>
    protected abstract TRequest CreateValidConcurrentRequest();
    
    /// <summary>
    /// Verify database consistency after concurrent operations
    /// Override to validate data consistency specific to your use case
    /// </summary>
    protected abstract Task VerifyDatabaseConsistencyAfterConcurrentOperations();
    
    #endregion
    
    #region Gateway Integration Contract Tests
    
    [Fact]
    public virtual async Task VerifyGatewayIntegration_WithPublicGateway_RoutesToCorrectApi()
    {
        // This will be implemented by concrete test classes that test gateway routing
        await VerifyPublicGatewayRouting();
        Output.WriteLine("✅ GATEWAY CONTRACT: Public Gateway correctly routes to Services Public API");
    }
    
    [Fact] 
    public virtual async Task VerifyGatewayIntegration_WithAdminGateway_RoutesToCorrectApiWithAuth()
    {
        // This will be implemented by concrete test classes that test admin gateway routing
        await VerifyAdminGatewayRouting();
        Output.WriteLine("✅ GATEWAY CONTRACT: Admin Gateway correctly routes to Services Admin API with authentication");
    }
    
    /// <summary>
    /// Verify Public Gateway routing to Services Public API
    /// Override to implement gateway routing tests specific to your use case
    /// </summary>
    protected abstract Task VerifyPublicGatewayRouting();
    
    /// <summary>
    /// Verify Admin Gateway routing to Services Admin API with authentication
    /// Override to implement admin gateway routing tests specific to your use case  
    /// </summary>
    protected abstract Task VerifyAdminGatewayRouting();
    
    #endregion
    
    #region Performance Contracts with Real Infrastructure
    
    [Fact]
    public virtual async Task VerifyExecuteAsync_WithValidRequest_CompletesWithinTimeLimitWithRealInfrastructure()
    {
        // Arrange - Setup real test data
        await SetupRealTestData();
        var validRequest = CreateValidRequest();
        
        // Act - Measure performance with real infrastructure
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await ExecuteUseCaseAsync(validRequest);
        stopwatch.Stop();
        
        // Assert - Performance contract with real infrastructure
        Assert.True(result.IsSuccess, "Use case should succeed for real performance test");
        
        // Real infrastructure may be slower than mocks, adjust timeout accordingly
        var timeoutMs = GetPerformanceTimeoutForRealInfrastructure();
        Assert.True(stopwatch.ElapsedMilliseconds < timeoutMs, 
            $"Performance contract violated: Use case took {stopwatch.ElapsedMilliseconds}ms (should be < {timeoutMs}ms with real infrastructure)");
        
        // Cleanup
        await CleanupRealTestData();
        
        Output.WriteLine($"✅ REAL PERFORMANCE CONTRACT: Use case completed in {stopwatch.ElapsedMilliseconds}ms with real infrastructure");
    }
    
    /// <summary>
    /// Get appropriate performance timeout for real infrastructure
    /// Override to set realistic timeouts for your use case with real database/services
    /// </summary>
    protected virtual int GetPerformanceTimeoutForRealInfrastructure() => 5000; // 5 seconds for real infrastructure
    
    #endregion
    
    #region Cleanup and Disposal
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ServiceScope?.Dispose();
        }
        base.Dispose(disposing);
    }
    
    #endregion
}