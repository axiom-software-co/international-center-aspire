namespace InternationalCenter.Tests.Shared.Contracts;

/// <summary>
/// Contract interface for testing repository implementations
/// Defines the behavioral contracts that all repositories must satisfy
/// Used for contract-first TDD to ensure consistent repository behavior across Services APIs
/// </summary>
/// <typeparam name="TEntity">The domain entity type</typeparam>
/// <typeparam name="TId">The entity identifier type</typeparam>
public interface IRepositoryContract<TEntity, TId> 
    where TEntity : class 
    where TId : notnull
{
    /// <summary>
    /// Contract: Repository must handle null entity gracefully
    /// Precondition: Entity parameter validation
    /// Postcondition: ArgumentNullException with proper message
    /// </summary>
    Task VerifyAddAsync_WithNullEntity_ThrowsArgumentNullException();
    
    /// <summary>
    /// Contract: Repository must successfully add valid entities
    /// Precondition: Valid entity with required properties
    /// Postcondition: Entity tracked in context with Added state
    /// </summary>
    Task VerifyAddAsync_WithValidEntity_AddsToContext();
    
    /// <summary>
    /// Contract: Repository must handle null ID gracefully
    /// Precondition: ID parameter validation
    /// Postcondition: ArgumentNullException with proper message
    /// </summary>
    Task VerifyGetByIdAsync_WithNullId_ThrowsArgumentNullException();
    
    /// <summary>
    /// Contract: Repository must return null for non-existent entities
    /// Precondition: Non-existent ID
    /// Postcondition: Returns null without throwing
    /// </summary>
    Task VerifyGetByIdAsync_WithNonExistentId_ReturnsNull();
    
    /// <summary>
    /// Contract: Repository must return entity with all required relationships loaded
    /// Precondition: Existing entity ID
    /// Postcondition: Entity returned with proper state and relationships
    /// </summary>
    Task VerifyGetByIdAsync_WithExistingId_ReturnsEntityWithRelationships();
    
    /// <summary>
    /// Contract: Repository must respect cancellation tokens
    /// Precondition: Cancelled cancellation token
    /// Postcondition: OperationCanceledException thrown
    /// </summary>
    Task VerifyOperations_WithCancelledToken_ThrowsOperationCanceledException();
    
    /// <summary>
    /// Contract: Repository must handle concurrent operations safely
    /// Precondition: Multiple concurrent operations
    /// Postcondition: All operations complete without data corruption
    /// </summary>
    Task VerifyConcurrentOperations_WithMultipleThreads_CompletesSuccessfully();
    
    /// <summary>
    /// Contract: Repository must log all operations for medical-grade audit
    /// Precondition: Any repository operation
    /// Postcondition: Structured log entries with correlation IDs and operation context
    /// </summary>
    Task VerifyOperations_WithAnyOperation_LogsAuditTrail();
}

/// <summary>
/// Contract interface for testing read-only repository implementations (Dapper-based)
/// Extends base repository contract with read-specific behaviors
/// Used for Services.Public.Api Dapper repositories
/// </summary>
/// <typeparam name="TEntity">The domain entity type</typeparam>
/// <typeparam name="TId">The entity identifier type</typeparam>
public interface IReadRepositoryContract<TEntity, TId> : IRepositoryContract<TEntity, TId>
    where TEntity : class 
    where TId : notnull
{
    /// <summary>
    /// Contract: Read repository must handle empty result sets gracefully
    /// Precondition: Database with no matching entities
    /// Postcondition: Empty collection returned, not null
    /// </summary>
    Task VerifyGetAllAsync_WithEmptyDatabase_ReturnsEmptyCollection();
    
    /// <summary>
    /// Contract: Read repository must apply proper ordering
    /// Precondition: Multiple entities with different sort criteria
    /// Postcondition: Entities returned in correct business-defined order
    /// </summary>
    Task VerifyGetAllAsync_WithMultipleEntities_ReturnsOrderedCollection();
    
    /// <summary>
    /// Contract: Read repository must handle pagination correctly
    /// Precondition: Pagination parameters (page, pageSize)
    /// Postcondition: Correct subset of entities and accurate total count
    /// </summary>
    Task VerifyGetPagedAsync_WithPaginationParameters_ReturnsCorrectPageAndCount();
    
    /// <summary>
    /// Contract: Read repository must apply search specifications correctly
    /// Precondition: Search specification with criteria
    /// Postcondition: Only entities matching specification returned
    /// </summary>
    Task VerifySearchAsync_WithSpecification_ReturnsMatchingEntitiesOnly();
    
    /// <summary>
    /// Contract: Read repository must perform efficiently for high-volume reads
    /// Precondition: Large dataset (performance test scenario)
    /// Postcondition: Query executes within acceptable time limits
    /// </summary>
    Task VerifyPerformance_WithLargeDataset_ExecutesWithinTimeLimit();
}

/// <summary>
/// Contract interface for testing use case implementations
/// Defines behavioral contracts for CQRS use cases in Services APIs
/// Focuses on business rule enforcement and error handling
/// </summary>
/// <typeparam name="TRequest">The use case request type</typeparam>
/// <typeparam name="TResponse">The use case response type</typeparam>
public interface IUseCaseContract<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    /// <summary>
    /// Contract: Use case must validate all input parameters
    /// Precondition: Null or invalid request
    /// Postcondition: Validation error result with descriptive message
    /// </summary>
    Task VerifyExecuteAsync_WithNullRequest_ReturnsValidationError();
    
    /// <summary>
    /// Contract: Use case must enforce business rules
    /// Precondition: Request violating business rules
    /// Postcondition: Business error result with appropriate error code
    /// </summary>
    Task VerifyExecuteAsync_WithBusinessRuleViolation_ReturnsBusinessError();
    
    /// <summary>
    /// Contract: Use case must handle repository failures gracefully
    /// Precondition: Repository operation throws exception
    /// Postcondition: Infrastructure error result without exposing internal details
    /// </summary>
    Task VerifyExecuteAsync_WithRepositoryFailure_ReturnsInfrastructureError();
    
    /// <summary>
    /// Contract: Use case must succeed with valid requests
    /// Precondition: Valid request with all required data
    /// Postcondition: Success result with expected response data
    /// </summary>
    Task VerifyExecuteAsync_WithValidRequest_ReturnsSuccessResult();
    
    /// <summary>
    /// Contract: Use case must log audit trail for medical-grade compliance
    /// Precondition: Any use case execution
    /// Postcondition: Audit logs with user context, operation details, and correlation IDs
    /// </summary>
    Task VerifyExecuteAsync_WithAnyRequest_LogsMedicalGradeAuditTrail();
    
    /// <summary>
    /// Contract: Use case must handle concurrent requests safely
    /// Precondition: Multiple concurrent requests
    /// Postcondition: All requests processed correctly without race conditions
    /// </summary>
    Task VerifyExecuteAsync_WithConcurrentRequests_HandlesAllRequestsSafely();
    
    /// <summary>
    /// Contract: Use case must respect cancellation tokens
    /// Precondition: Cancelled cancellation token
    /// Postcondition: OperationCanceledException or early termination
    /// </summary>
    Task VerifyExecuteAsync_WithCancelledToken_Respectscancellation();
}

/// <summary>
/// Contract interface for testing domain entity implementations
/// Ensures domain entities properly enforce business invariants
/// Medical-grade compliance requires strict invariant validation
/// </summary>
/// <typeparam name="TEntity">The domain entity type</typeparam>
public interface IDomainEntityContract<TEntity> where TEntity : class
{
    /// <summary>
    /// Contract: Entity constructor must validate all required parameters
    /// Precondition: Constructor parameters with null/invalid values
    /// Postcondition: ArgumentException with descriptive messages
    /// </summary>
    Task VerifyConstructor_WithInvalidParameters_ThrowsArgumentException();
    
    /// <summary>
    /// Contract: Entity must maintain domain invariants after state changes
    /// Precondition: Valid entity with state modification operations
    /// Postcondition: All domain invariants remain satisfied
    /// </summary>
    Task VerifyStateChanges_WithValidOperations_MaintainsDomainInvariants();
    
    /// <summary>
    /// Contract: Entity must reject invalid state transitions
    /// Precondition: Invalid state transition attempt
    /// Postcondition: InvalidOperationException with business rule explanation
    /// </summary>
    Task VerifyStateTransitions_WithInvalidTransition_ThrowsInvalidOperationException();
    
    /// <summary>
    /// Contract: Entity must update timestamps on modifications
    /// Precondition: Entity modification operation
    /// Postcondition: UpdatedAt timestamp reflects change time
    /// </summary>
    Task VerifyModifications_WithAnyChange_UpdatesTimestamps();
    
    /// <summary>
    /// Contract: Entity must provide consistent equality behavior
    /// Precondition: Two entities with same business key
    /// Postcondition: Equals returns true, GetHashCode returns same value
    /// </summary>
    Task VerifyEquality_WithSameBusinessKey_ReturnsTrueAndSameHashCode();
}