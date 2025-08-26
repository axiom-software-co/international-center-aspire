namespace InternationalCenter.Tests.Shared.Contracts;

/// <summary>
/// Contract interface for use case testing - ensures consistent contract testing across all use cases
/// Follows contract-first TDD principles with real implementations instead of mocks
/// Medical-grade testing approach focusing on business rule enforcement and audit compliance
/// </summary>
/// <typeparam name="TRequest">The use case request type</typeparam>
/// <typeparam name="TResponse">The use case response type</typeparam>
public interface IUseCaseContract<TRequest, TResponse> 
    where TRequest : class 
    where TResponse : class
{
    /// <summary>
    /// Contract test: Verify that null requests are properly handled with validation errors
    /// </summary>
    Task VerifyExecuteAsync_WithNullRequest_ReturnsValidationError();
    
    /// <summary>
    /// Contract test: Verify that invalid requests return appropriate validation errors
    /// </summary>
    Task VerifyExecuteAsync_WithInvalidRequests_ReturnsValidationErrors();
    
    /// <summary>
    /// Contract test: Verify that business rule violations are properly enforced
    /// </summary>
    Task VerifyExecuteAsync_WithBusinessRuleViolation_ReturnsBusinessError();
    
    /// <summary>
    /// Contract test: Verify that infrastructure failures are handled gracefully
    /// </summary>
    Task VerifyExecuteAsync_WithDatabaseConstraintViolation_ReturnsInfrastructureError();
    
    /// <summary>
    /// Contract test: Verify that valid requests return success results
    /// </summary>
    Task VerifyExecuteAsync_WithValidRequest_ReturnsSuccessResult();
    
    /// <summary>
    /// Contract test: Verify that medical-grade audit logging occurs for all operations
    /// </summary>
    Task VerifyExecuteAsync_WithAnyRequest_LogsRealMedicalGradeAuditTrail();
    
    /// <summary>
    /// Contract test: Verify that concurrent requests are handled safely
    /// </summary>
    Task VerifyExecuteAsync_WithConcurrentRequests_HandlesAllRequestsSafelyWithRealDb();
    
    /// <summary>
    /// Contract test: Verify that gateway routing works correctly for public operations
    /// </summary>
    Task VerifyGatewayIntegration_WithPublicGateway_RoutesToCorrectApi();
    
    /// <summary>
    /// Contract test: Verify that admin gateway routing works with authentication
    /// </summary>
    Task VerifyGatewayIntegration_WithAdminGateway_RoutesToCorrectApiWithAuth();
    
    /// <summary>
    /// Contract test: Verify that performance requirements are met with real infrastructure
    /// </summary>
    Task VerifyExecuteAsync_WithValidRequest_CompletesWithinTimeLimitWithRealInfrastructure();
}