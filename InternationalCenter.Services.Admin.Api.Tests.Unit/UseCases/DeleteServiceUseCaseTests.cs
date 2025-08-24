using InternationalCenter.Services.Admin.Api.Application.UseCases;
using InternationalCenter.Services.Domain.Entities;
using InternationalCenter.Services.Domain.Models;
using InternationalCenter.Services.Domain.Repositories;
using InternationalCenter.Services.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InternationalCenter.Services.Admin.Api.Tests.Unit.UseCases;

/// <summary>
/// TDD RED: Unit tests for DeleteServiceUseCase - medical-grade validation and audit logging
/// Tests the Use Case contract with comprehensive soft deletion and audit trail requirements
/// Validates medical-grade standards: soft deletion, service snapshots, deletion reasons, critical logging
/// </summary>
public class DeleteServiceUseCaseTests
{
    private readonly Mock<IServiceRepository> _mockServiceRepository;
    private readonly Mock<ILogger<DeleteServiceUseCase>> _mockLogger;
    private readonly DeleteServiceUseCase _useCase;
    private readonly Service _existingService;

    public DeleteServiceUseCaseTests()
    {
        _mockServiceRepository = new Mock<IServiceRepository>();
        _mockLogger = new Mock<ILogger<DeleteServiceUseCase>>();
        _useCase = new DeleteServiceUseCase(_mockServiceRepository.Object, _mockLogger.Object);

        // Create a test service for deletion scenarios
        _existingService = new Service(
            ServiceId.Create(),
            "Service To Delete",
            Slug.Create("service-to-delete"),
            "Service scheduled for deletion",
            "Detailed description of service to be deleted",
            ServiceMetadata.Create(
                technologies: new[] { "delete-test-tech" },
                features: new[] { "test-feature" },
                deliveryModes: new[] { "Online", "Offline" }
            )
        );
        
        // Publish the service to test deletion of active services
        _existingService.Publish();
        _existingService.SetFeatured(true);
    }

    [Fact(DisplayName = "TDD RED: DeleteService Should Fail With Null Request")]
    public async Task ExecuteAsync_WithNullRequest_ShouldReturnValidationFailure()
    {
        // ARRANGE: Null request (contract violation)
        DeleteServiceRequest? nullRequest = null;
        
        // ACT: Execute use case with invalid input
        var result = await _useCase.ExecuteAsync(nullRequest!);
        
        // ASSERT: Medical-grade validation should catch null input
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("VALIDATION_ERROR", result.Error.Code);
        Assert.Contains("request cannot be null", result.Error.Message.ToLowerInvariant());
    }

    [Fact(DisplayName = "TDD RED: DeleteService Should Fail With Empty ServiceId")]
    public async Task ExecuteAsync_WithEmptyServiceId_ShouldReturnValidationFailure()
    {
        // ARRANGE: Request with empty service ID (business rule violation)
        var request = new DeleteServiceRequest
        {
            ServiceId = "", // Invalid
            DeletionReason = "Testing deletion validation",
            RequestId = "test-request-id",
            UserContext = "admin-user",
            ClientIpAddress = "127.0.0.1",
            UserAgent = "Test-Agent"
        };
        
        // ACT: Execute use case with invalid service ID
        var result = await _useCase.ExecuteAsync(request);
        
        // ASSERT: Business validation should fail
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("INVALID_SERVICE_ID", result.Error.Code);
        Assert.Contains("service id", result.Error.Message.ToLowerInvariant());
    }

    [Fact(DisplayName = "TDD RED: DeleteService Should Fail With Missing UserContext")]
    public async Task ExecuteAsync_WithMissingUserContext_ShouldReturnValidationFailure()
    {
        // ARRANGE: Request missing medical-grade audit context
        var request = new DeleteServiceRequest
        {
            ServiceId = "test-service-id",
            DeletionReason = "Test deletion",
            RequestId = "test-request-id",
            UserContext = null!, // Missing audit context
            ClientIpAddress = "127.0.0.1",
            UserAgent = "Test-Agent"
        };
        
        // ACT: Execute use case with missing audit data
        var result = await _useCase.ExecuteAsync(request);
        
        // ASSERT: Medical-grade audit validation should fail
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("MISSING_USER_CONTEXT", result.Error.Code);
        Assert.Contains("user context", result.Error.Message.ToLowerInvariant());
    }

    [Fact(DisplayName = "TDD RED: DeleteService Should Fail With Missing Deletion Reason")]
    public async Task ExecuteAsync_WithMissingDeletionReason_ShouldReturnValidationFailure()
    {
        // ARRANGE: Request missing deletion reason (medical-grade audit requirement)
        var request = new DeleteServiceRequest
        {
            ServiceId = "test-service-id",
            DeletionReason = "", // Missing deletion reason
            RequestId = "test-request-id",
            UserContext = "admin-user",
            ClientIpAddress = "127.0.0.1",
            UserAgent = "Test-Agent"
        };
        
        // ACT: Execute use case with missing deletion reason
        var result = await _useCase.ExecuteAsync(request);
        
        // ASSERT: Medical-grade audit validation should fail
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("MISSING_DELETION_REASON", result.Error.Code);
        Assert.Contains("deletion reason", result.Error.Message.ToLowerInvariant());
    }

    [Fact(DisplayName = "TDD RED: DeleteService Should Fail When Service Not Found")]
    public async Task ExecuteAsync_WithNonExistentService_ShouldReturnNotFoundError()
    {
        // ARRANGE: Valid request but service doesn't exist
        var request = new DeleteServiceRequest
        {
            ServiceId = "non-existent-id",
            DeletionReason = "Service no longer needed",
            RequestId = "test-request-id",
            UserContext = "admin-user",
            ClientIpAddress = "127.0.0.1",
            UserAgent = "Test-Agent"
        };
        
        _mockServiceRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<ServiceId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Service?)null);
        
        // ACT: Execute use case with non-existent service
        var result = await _useCase.ExecuteAsync(request);
        
        // ASSERT: Should return not found error
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("SERVICE_NOT_FOUND", result.Error.Code);
        Assert.Contains("non-existent-id", result.Error.Message);
    }

    [Fact(DisplayName = "TDD RED: DeleteService Should Handle Repository Failure Gracefully")]
    public async Task ExecuteAsync_WhenRepositoryFails_ShouldReturnRepositoryFailure()
    {
        // ARRANGE: Valid request but repository failure
        var request = new DeleteServiceRequest
        {
            ServiceId = _existingService.Id,
            DeletionReason = "Testing repository failure handling",
            RequestId = "test-request-id",
            UserContext = "admin-user",
            ClientIpAddress = "127.0.0.1",
            UserAgent = "Test-Agent"
        };
        
        _mockServiceRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<ServiceId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_existingService);
        
        _mockServiceRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));
        
        // ACT: Execute use case with repository failure
        var result = await _useCase.ExecuteAsync(request);
        
        // ASSERT: Should handle repository errors gracefully
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("SERVICE_DELETION_FAILED", result.Error.Code);
        Assert.Contains("internal error", result.Error.Message.ToLowerInvariant());
    }

    [Fact(DisplayName = "TDD RED: DeleteService Should Perform Soft Delete Successfully")]
    public async Task ExecuteAsync_WithValidRequest_ShouldPerformSoftDeleteWithAuditTrail()
    {
        // ARRANGE: Valid deletion request
        var request = new DeleteServiceRequest
        {
            ServiceId = _existingService.Id,
            DeletionReason = "Service is obsolete and no longer maintained",
            RequestId = "test-delete-request-123",
            UserContext = "admin@example.com",
            ClientIpAddress = "192.168.1.100",
            UserAgent = "Mozilla/5.0 Admin Agent"
        };
        
        _mockServiceRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<ServiceId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_existingService);
        
        _mockServiceRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        _mockServiceRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));
        
        // ACT: Execute soft deletion
        var result = await _useCase.ExecuteAsync(request);
        
        // ASSERT: Should succeed with soft deletion
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(_existingService.Id, result.Value.ServiceId);
        Assert.Equal("SOFT_DELETE", result.Value.DeletionType);
        Assert.True(result.Value.DeletedAt <= DateTime.UtcNow);
        
        // Verify service snapshot is captured
        Assert.NotNull(result.Value.ServiceSnapshot);
        Assert.Equal(_existingService.Title, result.Value.ServiceSnapshot.Title);
        Assert.Equal(_existingService.Slug, result.Value.ServiceSnapshot.Slug);
        Assert.Equal(_existingService.Description, result.Value.ServiceSnapshot.Description);
        
        // Verify audit trail
        Assert.NotEmpty(result.Value.AuditTrail);
        Assert.Contains(result.Value.AuditTrail, entry => entry.Operation == "DELETE_SERVICE_STARTED");
        Assert.Contains(result.Value.AuditTrail, entry => entry.Operation == "DELETE_SERVICE_COMPLETED");
        Assert.All(result.Value.AuditTrail, entry => Assert.Equal("SOFT_DELETE", entry.OperationType));
        
        // Verify performance metrics
        Assert.NotNull(result.Value.PerformanceMetrics);
        Assert.True(result.Value.PerformanceMetrics.TotalDuration > TimeSpan.Zero);
        
        // Verify repository interaction (Archive() calls UpdateAsync, not DeleteAsync)
        _mockServiceRepository.Verify(r => r.GetByIdAsync(It.IsAny<ServiceId>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockServiceRepository.Verify(r => r.UpdateAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockServiceRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "TDD RED: DeleteService Should Capture Complete Service Snapshot")]
    public async Task ExecuteAsync_ShouldCaptureCompleteServiceSnapshot()
    {
        // ARRANGE: Service with complete metadata for snapshot testing
        var request = new DeleteServiceRequest
        {
            ServiceId = _existingService.Id,
            DeletionReason = "Testing service snapshot capture",
            RequestId = "snapshot-test-123",
            UserContext = "admin@example.com",
            ClientIpAddress = "10.0.0.1",
            UserAgent = "Snapshot-Test-Agent"
        };
        
        _mockServiceRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<ServiceId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_existingService);
        
        _mockServiceRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        _mockServiceRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));
        
        // ACT: Execute deletion
        var result = await _useCase.ExecuteAsync(request);
        
        // ASSERT: Should capture complete service state before deletion
        Assert.True(result.IsSuccess);
        var snapshot = result.Value.ServiceSnapshot;
        
        Assert.Equal(_existingService.Id, snapshot.Id);
        Assert.Equal(_existingService.Title, snapshot.Title);
        Assert.Equal(_existingService.Slug, snapshot.Slug);
        Assert.Equal(_existingService.Description, snapshot.Description);
        Assert.Equal(_existingService.Status.ToString(), snapshot.Status);
        Assert.Equal(_existingService.Available, snapshot.Available);
        Assert.Equal(_existingService.Featured, snapshot.Featured);
        Assert.Equal(_existingService.CreatedAt, snapshot.CreatedAt);
        Assert.Equal(_existingService.UpdatedAt, snapshot.LastModifiedAt);
    }

    [Fact(DisplayName = "TDD RED: DeleteService Should Log Critical Medical-Grade Audit Trail")]
    public async Task ExecuteAsync_ShouldLogCriticalMedicalGradeAuditTrail()
    {
        // ARRANGE: Request that will trigger critical audit logging
        var request = new DeleteServiceRequest
        {
            ServiceId = _existingService.Id,
            DeletionReason = "GDPR compliance - user data deletion request",
            RequestId = "critical-audit-delete-123",
            UserContext = "compliance-admin@test.com",
            ClientIpAddress = "10.0.0.1",
            UserAgent = "Compliance-Audit-Agent/1.0"
        };
        
        _mockServiceRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<ServiceId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_existingService);
        
        _mockServiceRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        _mockServiceRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));
        
        // ACT: Execute use case
        var result = await _useCase.ExecuteAsync(request);
        
        // ASSERT: Verify critical medical-grade audit logging occurred
        Assert.True(result.IsSuccess);
        
        // Verify critical logging for deletion operations (should use LogCritical)
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Critical,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("DELETE_SERVICE_STARTED") && 
                                             v.ToString()!.Contains(request.ServiceId)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        
        // Verify critical completion logging
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Critical,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("DELETE_SERVICE_COMPLETED") && 
                                             v.ToString()!.Contains(request.ServiceId)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact(DisplayName = "TDD RED: DeleteService Should Include Deletion Reason In Audit")]
    public async Task ExecuteAsync_ShouldIncludeDeletionReasonInAuditTrail()
    {
        // ARRANGE: Request with specific deletion reason
        var deletionReason = "Service deprecated due to security vulnerabilities";
        var request = new DeleteServiceRequest
        {
            ServiceId = _existingService.Id,
            DeletionReason = deletionReason,
            RequestId = "deletion-reason-audit-123",
            UserContext = "security-admin@example.com",
            ClientIpAddress = "192.168.1.200",
            UserAgent = "Security-Admin-Tool"
        };
        
        _mockServiceRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<ServiceId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_existingService);
        
        _mockServiceRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        _mockServiceRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));
        
        // ACT: Execute deletion
        var result = await _useCase.ExecuteAsync(request);
        
        // ASSERT: Should include deletion reason in audit trail
        Assert.True(result.IsSuccess);
        
        var startAuditEntry = result.Value.AuditTrail.FirstOrDefault(e => e.Operation == "DELETE_SERVICE_STARTED");
        Assert.NotNull(startAuditEntry);
        Assert.Contains(deletionReason, startAuditEntry.Changes);
        Assert.True(startAuditEntry.Metadata.ContainsKey("DeletionReason"));
        Assert.Equal(deletionReason, startAuditEntry.Metadata["DeletionReason"]);
        Assert.Equal("SOFT_DELETE", startAuditEntry.Metadata["DeletionType"]);
    }

    [Fact(DisplayName = "TDD RED: DeleteService Should Handle Force Delete Flag")]
    public async Task ExecuteAsync_WithForceDeleteFlag_ShouldAuditForceDeleteAttempt()
    {
        // ARRANGE: Request with force delete flag (for future hard delete functionality)
        var request = new DeleteServiceRequest
        {
            ServiceId = _existingService.Id,
            DeletionReason = "Emergency deletion required",
            ForceDelete = true, // Force delete flag
            RequestId = "force-delete-test-123",
            UserContext = "emergency-admin@example.com",
            ClientIpAddress = "192.168.1.999",
            UserAgent = "Emergency-Admin-Tool"
        };
        
        _mockServiceRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<ServiceId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_existingService);
        
        _mockServiceRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        _mockServiceRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));
        
        // ACT: Execute force deletion
        var result = await _useCase.ExecuteAsync(request);
        
        // ASSERT: Should still perform soft delete but audit the force delete flag
        Assert.True(result.IsSuccess);
        Assert.Equal("SOFT_DELETE", result.Value.DeletionType); // Still soft delete for now
        
        // Force delete flag should be preserved in audit
        var auditEntries = result.Value.AuditTrail;
        Assert.NotEmpty(auditEntries);
        Assert.All(auditEntries, entry => Assert.Equal("SOFT_DELETE", entry.OperationType));
    }

    [Fact(DisplayName = "TDD RED: DeleteService Should Handle Concurrent Deletions")]
    public async Task ExecuteAsync_WithConcurrentDeletions_ShouldHandleGracefully()
    {
        // ARRANGE: Multiple concurrent deletion requests for different services
        var services = Enumerable.Range(1, 3).Select(i => 
        {
            var service = new Service(
                ServiceId.Create(),
                $"Service {i}",
                Slug.Create($"service-{i}"),
                $"Description {i}",
                $"Detailed description {i}",
                ServiceMetadata.Create()
            );
            service.Publish(); // Make services active before deletion
            return service;
        }).ToList();

        var requests = services.Select((service, index) => new DeleteServiceRequest
        {
            ServiceId = service.Id,
            DeletionReason = $"Concurrent deletion test {index + 1}",
            RequestId = $"concurrent-delete-{index + 1}",
            UserContext = $"user-{index + 1}@test.com",
            ClientIpAddress = "127.0.0.1",
            UserAgent = "Concurrent-Delete-Agent"
        }).ToList();
        
        // Setup repository to return the correct service for each request
        for (int i = 0; i < services.Count; i++)
        {
            var service = services[i];
            _mockServiceRepository
                .Setup(r => r.GetByIdAsync(ServiceId.Create(service.Id), It.IsAny<CancellationToken>()))
                .ReturnsAsync(service);
        }
        
        _mockServiceRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        _mockServiceRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));
        
        // ACT: Execute concurrent deletions
        var tasks = requests.Select(r => _useCase.ExecuteAsync(r)).ToArray();
        var results = await Task.WhenAll(tasks);
        
        // ASSERT: All deletions should succeed
        Assert.All(results, result => Assert.True(result.IsSuccess));
        Assert.Equal(3, results.Length);
        
        // Verify each deletion has proper audit trail
        foreach (var result in results)
        {
            Assert.NotEmpty(result.Value.AuditTrail);
            Assert.Contains(result.Value.AuditTrail, entry => entry.Operation == "DELETE_SERVICE_STARTED");
            Assert.Contains(result.Value.AuditTrail, entry => entry.Operation == "DELETE_SERVICE_COMPLETED");
        }
        
        // Verify repository was called for each deletion
        _mockServiceRepository.Verify(r => r.UpdateAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        _mockServiceRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
    }
}