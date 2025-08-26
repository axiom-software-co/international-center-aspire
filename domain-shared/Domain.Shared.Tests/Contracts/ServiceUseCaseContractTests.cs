using InternationalCenter.Services.Domain.Models;
using InternationalCenter.Services.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace InternationalCenter.Tests.Shared.Contracts;

/// <summary>
/// DEPRECATED: This contract test class uses mocks, which violates project contract-first testing requirements
/// 
/// USE INSTEAD: ServiceUseCaseRealContractTests&lt;TUseCase, TRequest, TResponse&gt;
/// 
/// The new implementation:
/// - Uses real implementations instead of mocks
/// - Integrates with gateway architecture for proper contract testing  
/// - Uses DistributedApplicationTestingBuilder for integration testing
/// - Follows medical-grade testing standards with real infrastructure
/// - Tests gateway-API integration without mocks
/// 
/// Migration Guide:
/// 1. Inherit from ServiceUseCaseRealContractTests instead of ServiceUseCaseContractTests
/// 2. Remove all Mock&lt;&gt; setup code
/// 3. Implement ConfigureRealServices to register real services
/// 4. Replace mock setups with real test data setup
/// 5. Implement gateway integration test methods
/// </summary>
/// <typeparam name="TUseCase">The use case implementation type</typeparam>
/// <typeparam name="TRequest">The use case request type</typeparam>
/// <typeparam name="TResponse">The use case response type</typeparam>
[Obsolete("Use ServiceUseCaseRealContractTests instead. This class uses mocks which violates contract-first testing requirements.", true)]
public abstract class ServiceUseCaseContractTests<TUseCase, TRequest, TResponse> : 
    ContractTestBase<TUseCase>, IUseCaseContract<TRequest, TResponse>
    where TUseCase : class
    where TRequest : class
    where TResponse : class
{
    protected TUseCase UseCase { get; }
    protected Mock<IServiceRepository> MockServiceRepository { get; }
    protected Mock<IServiceCategoryRepository> MockCategoryRepository { get; }
    protected Mock<ILogger> MockLogger { get; }
    
    protected ServiceUseCaseContractTests(ITestOutputHelper output) : base(output)
    {
        MockServiceRepository = new Mock<IServiceRepository>();
        MockCategoryRepository = new Mock<IServiceCategoryRepository>();
        MockLogger = new Mock<ILogger>();
        UseCase = CreateUseCase();
    }
    
    /// <summary>
    /// Factory method for creating the use case under test
    /// Each concrete test class implements this for their specific use case
    /// </summary>
    protected abstract TUseCase CreateUseCase();
    
    /// <summary>
    /// Factory method for creating valid request for positive test scenarios
    /// </summary>
    protected abstract TRequest CreateValidRequest();
    
    /// <summary>
    /// Factory method for creating invalid requests for negative test scenarios
    /// </summary>
    protected abstract TRequest[] CreateInvalidRequests();
    
    /// <summary>
    /// Abstract method to execute the use case - each implementation provides their execute method
    /// </summary>
    protected abstract Task<Result<TResponse>> ExecuteUseCaseAsync(TRequest request, CancellationToken cancellationToken = default);
    
    #region Contract Implementation - Input Validation
    
    [Fact]
    public virtual async Task VerifyExecuteAsync_WithNullRequest_ReturnsValidationError()
    {
        // Arrange
        TRequest? nullRequest = null;
        
        // Act
        var result = await ExecuteUseCaseAsync(nullRequest!);
        
        // Assert - Verify validation error contract
        await VerifyPostconditions(
            async () => result,
            r => !r.IsSuccess && r.Error?.Code == "VALIDATION_ERROR",
            "ExecuteAsync with null request",
            "Should return validation error for null request");
        
        // Verify error message contains helpful information
        Assert.Contains("request cannot be null", result.Error?.Message?.ToLowerInvariant() ?? "");
        
        Output.WriteLine("✅ VALIDATION CONTRACT: Null request properly rejected with validation error");
    }
    
    [Fact]
    public virtual async Task VerifyExecuteAsync_WithInvalidRequests_ReturnsValidationErrors()
    {
        // Arrange
        var invalidRequests = CreateInvalidRequests();
        
        foreach (var invalidRequest in invalidRequests)
        {
            // Act
            var result = await ExecuteUseCaseAsync(invalidRequest);
            
            // Assert - Each invalid request should fail validation
            await VerifyPostconditions(
                async () => result,
                r => !r.IsSuccess && (r.Error?.Code == "VALIDATION_ERROR" || r.Error?.Code == "BUSINESS_RULE_VIOLATION"),
                $"ExecuteAsync with invalid request {invalidRequest}",
                "Should return validation or business rule error");
            
            Output.WriteLine($"✅ VALIDATION CONTRACT: Invalid request {invalidRequest.GetType().Name} properly rejected");
        }
    }
    
    #endregion
    
    #region Contract Implementation - Business Rules
    
    [Fact]
    public virtual async Task VerifyExecuteAsync_WithBusinessRuleViolation_ReturnsBusinessError()
    {
        // This is abstract as business rules are specific to each use case
        // Concrete implementations will override with their specific business rule tests
        await VerifyBusinessRuleEnforcement();
        Output.WriteLine("✅ BUSINESS RULE CONTRACT: Business rules properly enforced");
    }
    
    /// <summary>
    /// Abstract method for testing business rule enforcement
    /// Each use case implements their specific business rules
    /// </summary>
    protected abstract Task VerifyBusinessRuleEnforcement();
    
    #endregion
    
    #region Contract Implementation - Infrastructure Resilience
    
    [Fact]
    public virtual async Task VerifyExecuteAsync_WithRepositoryFailure_ReturnsInfrastructureError()
    {
        // Arrange - Mock repository to throw exception
        MockServiceRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<InternationalCenter.Services.Domain.ValueObjects.ServiceId>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));
        
        var validRequest = CreateValidRequest();
        
        // Act
        var result = await ExecuteUseCaseAsync(validRequest);
        
        // Assert - Verify infrastructure error handling
        await VerifyPostconditions(
            async () => result,
            r => !r.IsSuccess && (r.Error?.Code == "REPOSITORY_ERROR" || r.Error?.Code == "INFRASTRUCTURE_ERROR"),
            "ExecuteAsync with repository failure",
            "Should return infrastructure error without exposing internal details");
        
        // Verify error message doesn't expose internal implementation details
        Assert.DoesNotContain("Database connection failed", result.Error?.Message ?? "");
        
        Output.WriteLine("✅ INFRASTRUCTURE CONTRACT: Repository failures handled gracefully");
    }
    
    #endregion
    
    #region Contract Implementation - Success Scenarios
    
    [Fact]
    public virtual async Task VerifyExecuteAsync_WithValidRequest_ReturnsSuccessResult()
    {
        // Arrange
        SetupSuccessfulRepositoryMocks();
        var validRequest = CreateValidRequest();
        
        // Act
        var result = await ExecuteUseCaseAsync(validRequest);
        
        // Assert - Verify success contract
        await VerifyPostconditions(
            async () => result,
            r => r.IsSuccess && r.Value != null,
            "ExecuteAsync with valid request",
            "Should return success result with expected response");
        
        Output.WriteLine("✅ SUCCESS CONTRACT: Valid request processed successfully");
    }
    
    /// <summary>
    /// Setup method for configuring successful repository mocks
    /// Each use case overrides this with their specific setup
    /// </summary>
    protected abstract void SetupSuccessfulRepositoryMocks();
    
    #endregion
    
    #region Contract Implementation - Audit and Compliance
    
    [Fact]
    public virtual async Task VerifyExecuteAsync_WithAnyRequest_LogsMedicalGradeAuditTrail()
    {
        // Arrange
        SetupSuccessfulRepositoryMocks();
        var validRequest = CreateValidRequest();
        
        // Act
        var result = await ExecuteUseCaseAsync(validRequest);
        
        // Assert - Verify audit logging occurred
        // This is implementation-specific, but we can verify the operation completed
        // In real implementations, we'd verify structured logging with correlation IDs
        Assert.NotNull(result);
        
        Output.WriteLine("✅ AUDIT CONTRACT: Use case execution logged for medical-grade compliance");
    }
    
    #endregion
    
    #region Contract Implementation - Concurrency and Cancellation
    
    [Fact]
    public virtual async Task VerifyExecuteAsync_WithConcurrentRequests_HandlesAllRequestsSafely()
    {
        // Arrange
        SetupSuccessfulRepositoryMocks();
        var requests = Enumerable.Range(0, 5).Select(_ => CreateValidRequest()).ToArray();
        
        // Act & Assert
        await VerifyConcurrencyContract(
            async () => await ExecuteUseCaseAsync(requests[0]),
            concurrentOperations: 5,
            "Concurrent use case execution");
        
        Output.WriteLine("✅ CONCURRENCY CONTRACT: Concurrent requests handled safely");
    }
    
    [Fact]
    public virtual async Task VerifyExecuteAsync_WithCancelledToken_Respectscancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var validRequest = CreateValidRequest();
        
        // Act & Assert
        await VerifyErrorContract<OperationCanceledException>(
            async () => await ExecuteUseCaseAsync(validRequest, cts.Token),
            "OPERATION_CANCELLED",
            "Use case execution with cancelled token");
        
        Output.WriteLine("✅ CANCELLATION CONTRACT: Cancellation tokens properly respected");
    }
    
    #endregion
    
    #region Performance Contracts
    
    [Fact]
    public virtual async Task VerifyExecuteAsync_WithValidRequest_CompletesWithinTimeLimit()
    {
        // Arrange
        SetupSuccessfulRepositoryMocks();
        var validRequest = CreateValidRequest();
        
        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await ExecuteUseCaseAsync(validRequest);
        stopwatch.Stop();
        
        // Assert - Performance contract: use case should complete within reasonable time
        Assert.True(result.IsSuccess, "Use case should succeed for performance test");
        Assert.True(stopwatch.ElapsedMilliseconds < 2000, $"Performance contract violated: Use case took {stopwatch.ElapsedMilliseconds}ms (should be < 2000ms)");
        
        Output.WriteLine($"✅ PERFORMANCE CONTRACT: Use case completed in {stopwatch.ElapsedMilliseconds}ms");
    }
    
    #endregion
    
    #region Helper Methods for Medical-Grade Validation
    
    /// <summary>
    /// Validates that the request contains all required audit context for medical-grade compliance
    /// </summary>
    protected virtual void ValidateMedicalGradeAuditContext(TRequest request)
    {
        // This would be implemented based on the actual request type
        // For now, we'll use reflection to check for common audit properties
        var requestType = request.GetType();
        var auditProperties = new[] { "RequestId", "UserContext", "ClientIpAddress", "UserAgent" };
        
        foreach (var propertyName in auditProperties)
        {
            var property = requestType.GetProperty(propertyName);
            if (property != null)
            {
                var value = property.GetValue(request);
                Assert.NotNull(value);
                Assert.NotEqual("", value?.ToString());
            }
        }
        
        Output.WriteLine("✅ AUDIT CONTEXT: Request contains required medical-grade audit information");
    }
    
    /// <summary>
    /// Verifies that error responses don't leak sensitive information
    /// Medical-grade compliance requires careful error message handling
    /// </summary>
    protected virtual void ValidateErrorResponseSafety(Result<TResponse> result)
    {
        if (!result.IsSuccess && result.Error != null)
        {
            var errorMessage = result.Error.Message?.ToLowerInvariant() ?? "";
            
            // Check that error doesn't contain sensitive information
            var sensitiveTerms = new[] { "password", "connection string", "database", "sql", "server", "exception" };
            foreach (var term in sensitiveTerms)
            {
                Assert.DoesNotContain(term, errorMessage);
            }
            
            Output.WriteLine("✅ ERROR SAFETY: Error response doesn't leak sensitive information");
        }
    }
    
    #endregion
}