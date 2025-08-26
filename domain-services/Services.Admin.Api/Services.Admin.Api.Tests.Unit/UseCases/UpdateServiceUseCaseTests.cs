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
/// TDD RED: Unit tests for UpdateServiceUseCase - medical-grade validation and audit logging
/// Tests the Use Case contract with comprehensive change tracking and error scenarios
/// Validates medical-grade standards: partial updates, audit trails, performance metrics
/// </summary>
public class UpdateServiceUseCaseTests
{
    private readonly Mock<IServiceRepository> _mockServiceRepository;
    private readonly Mock<ILogger<UpdateServiceUseCase>> _mockLogger;
    private readonly UpdateServiceUseCase _useCase;
    private readonly Service _existingService;

    public UpdateServiceUseCaseTests()
    {
        _mockServiceRepository = new Mock<IServiceRepository>();
        _mockLogger = new Mock<ILogger<UpdateServiceUseCase>>();
        _useCase = new UpdateServiceUseCase(_mockServiceRepository.Object, _mockLogger.Object);

        // Create a test service for update scenarios
        _existingService = new Service(
            ServiceId.Create(),
            "Original Title",
            Slug.Create("original-title"),
            "Original description",
            "Original detailed description",
            ServiceMetadata.Create(
                technologies: new[] { "original-tech" },
                features: new[] { "original-feature" },
                deliveryModes: new[] { "Online" }
            )
        );
    }

    [Fact(DisplayName = "TDD RED: UpdateService Should Fail With Null Request", Timeout = 5000)]
    public async Task ExecuteAsync_WithNullRequest_ShouldReturnValidationFailure()
    {
        // ARRANGE: Null request (contract violation)
        UpdateServiceRequest? nullRequest = null;
        
        // ACT: Execute use case with invalid input
        var result = await _useCase.ExecuteAsync(nullRequest!);
        
        // ASSERT: Medical-grade validation should catch null input
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("VALIDATION_ERROR", result.Error.Code);
        Assert.Contains("request cannot be null", result.Error.Message.ToLowerInvariant());
    }

    [Fact(DisplayName = "TDD RED: UpdateService Should Fail With Empty ServiceId", Timeout = 5000)]
    public async Task ExecuteAsync_WithEmptyServiceId_ShouldReturnValidationFailure()
    {
        // ARRANGE: Request with empty service ID (business rule violation)
        var request = new UpdateServiceRequest
        {
            ServiceId = "", // Invalid
            Title = "Updated Title",
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

    [Fact(DisplayName = "TDD RED: UpdateService Should Fail With Missing UserContext", Timeout = 5000)]
    public async Task ExecuteAsync_WithMissingUserContext_ShouldReturnValidationFailure()
    {
        // ARRANGE: Request missing medical-grade audit context
        var request = new UpdateServiceRequest
        {
            ServiceId = "test-service-id",
            Title = "Updated Title",
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

    [Fact(DisplayName = "TDD RED: UpdateService Should Fail With Excessively Long Title", Timeout = 5000)]
    public async Task ExecuteAsync_WithTooLongTitle_ShouldReturnValidationFailure()
    {
        // ARRANGE: Request with title exceeding maximum length
        var request = new UpdateServiceRequest
        {
            ServiceId = "test-service-id",
            Title = new string('a', 201), // Exceeds 200 character limit
            RequestId = "test-request-id",
            UserContext = "admin-user",
            ClientIpAddress = "127.0.0.1",
            UserAgent = "Test-Agent"
        };
        
        // ACT: Execute use case with invalid title length
        var result = await _useCase.ExecuteAsync(request);
        
        // ASSERT: Business validation should fail
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("TITLE_TOO_LONG", result.Error.Code);
        Assert.Contains("200 characters", result.Error.Message);
    }

    [Fact(DisplayName = "TDD RED: UpdateService Should Fail When Service Not Found", Timeout = 5000)]
    public async Task ExecuteAsync_WithNonExistentService_ShouldReturnNotFoundError()
    {
        // ARRANGE: Valid request but service doesn't exist
        var request = new UpdateServiceRequest
        {
            ServiceId = "non-existent-id",
            Title = "Updated Title",
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

    [Fact(DisplayName = "TDD RED: UpdateService Should Handle Repository Failure Gracefully", Timeout = 5000)]
    public async Task ExecuteAsync_WhenRepositoryFails_ShouldReturnRepositoryFailure()
    {
        // ARRANGE: Valid request but repository failure
        var request = new UpdateServiceRequest
        {
            ServiceId = _existingService.Id,
            Title = "Updated Title",
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
        Assert.Equal("SERVICE_UPDATE_FAILED", result.Error.Code);
        Assert.Contains("internal error", result.Error.Message.ToLowerInvariant());
    }

    [Fact(DisplayName = "TDD RED: UpdateService Should Succeed With Valid Title Update", Timeout = 5000)]
    public async Task ExecuteAsync_WithValidTitleUpdate_ShouldReturnSuccessWithChanges()
    {
        // ARRANGE: Valid request with title update
        var request = new UpdateServiceRequest
        {
            ServiceId = _existingService.Id,
            Title = "Updated Service Title",
            RequestId = "test-request-id-123",
            UserContext = "admin@example.com",
            ClientIpAddress = "192.168.1.100",
            UserAgent = "Mozilla/5.0 Test Agent"
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
        
        // ACT: Execute use case with valid title update
        var result = await _useCase.ExecuteAsync(request);
        
        // ASSERT: Should succeed with change tracking
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotNull(result.Value.Changes);
        Assert.True(result.Value.Changes.ContainsKey("Title"));
        Assert.Equal("Original Title", result.Value.Changes["Title"].OldValue);
        Assert.Equal("Updated Service Title", result.Value.Changes["Title"].NewValue);
        
        // Verify audit trail
        Assert.NotEmpty(result.Value.AuditTrail);
        Assert.Contains(result.Value.AuditTrail, entry => entry.Operation == "UPDATE_SERVICE_STARTED");
        Assert.Contains(result.Value.AuditTrail, entry => entry.Operation == "UPDATE_SERVICE_COMPLETED");
        
        // Verify performance metrics
        Assert.NotNull(result.Value.PerformanceMetrics);
        Assert.True(result.Value.PerformanceMetrics.TotalDuration > TimeSpan.Zero);
        
        // Verify repository interaction
        _mockServiceRepository.Verify(r => r.GetByIdAsync(It.IsAny<ServiceId>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockServiceRepository.Verify(r => r.UpdateAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockServiceRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "TDD RED: UpdateService Should Succeed With Availability Toggle", Timeout = 5000)]
    public async Task ExecuteAsync_WithAvailabilityToggle_ShouldReturnSuccessWithChanges()
    {
        // ARRANGE: Request to toggle availability
        var request = new UpdateServiceRequest
        {
            ServiceId = _existingService.Id,
            Available = false, // Toggle from true to false
            RequestId = "test-request-availability",
            UserContext = "admin@example.com",
            ClientIpAddress = "10.0.0.1",
            UserAgent = "Admin-Panel/1.0"
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
        
        // ACT: Execute availability update
        var result = await _useCase.ExecuteAsync(request);
        
        // ASSERT: Should succeed with availability change
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.Changes.ContainsKey("Available"));
        Assert.Equal(true, result.Value.Changes["Available"].OldValue);
        Assert.Equal(false, result.Value.Changes["Available"].NewValue);
    }

    [Fact(DisplayName = "TDD RED: UpdateService Should Log Medical-Grade Audit Trail", Timeout = 5000)]
    public async Task ExecuteAsync_ShouldLogMedicalGradeAuditTrail()
    {
        // ARRANGE: Request that will trigger comprehensive audit logging
        var request = new UpdateServiceRequest
        {
            ServiceId = _existingService.Id,
            Title = "Audit Test Update",
            Description = "Updated description for audit testing",
            RequestId = "audit-request-update-123",
            UserContext = "audit-admin@test.com",
            ClientIpAddress = "10.0.0.1",
            UserAgent = "Audit-Test-Agent/1.0"
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
        
        // ASSERT: Verify medical-grade audit logging occurred
        Assert.True(result.IsSuccess);
        
        // Verify audit start logging
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("UPDATE_SERVICE_STARTED")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        
        // Verify audit completion logging
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("UPDATE_SERVICE_COMPLETED")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact(DisplayName = "TDD RED: UpdateService Should Handle Multiple Field Updates", Timeout = 5000)]
    public async Task ExecuteAsync_WithMultipleFieldUpdates_ShouldTrackAllChanges()
    {
        // ARRANGE: Request updating multiple fields
        var request = new UpdateServiceRequest
        {
            ServiceId = _existingService.Id,
            Title = "Multi-Field Update",
            Description = "New description",
            DetailedDescription = "New detailed description",
            Available = false,
            RequestId = "multi-field-update-123",
            UserContext = "admin@example.com",
            ClientIpAddress = "192.168.1.50",
            UserAgent = "Multi-Update-Agent"
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
        
        // ACT: Execute multi-field update
        var result = await _useCase.ExecuteAsync(request);
        
        // ASSERT: Should track all changes
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        
        // Verify all expected changes are tracked
        Assert.True(result.Value.Changes.ContainsKey("Title"));
        Assert.True(result.Value.Changes.ContainsKey("Description"));
        Assert.True(result.Value.Changes.ContainsKey("DetailedDescription"));
        Assert.True(result.Value.Changes.ContainsKey("Available"));
        
        // Verify old values are preserved
        Assert.Equal("Original Title", result.Value.Changes["Title"].OldValue);
        Assert.Equal("Original description", result.Value.Changes["Description"].OldValue);
        Assert.Equal("Original detailed description", result.Value.Changes["DetailedDescription"].OldValue);
        Assert.Equal(true, result.Value.Changes["Available"].OldValue);
    }

    [Fact(DisplayName = "TDD RED: UpdateService Should Handle Partial Updates", Timeout = 5000)]
    public async Task ExecuteAsync_WithPartialUpdate_ShouldOnlyUpdateProvidedFields()
    {
        // ARRANGE: Request with only title update (partial update)
        var request = new UpdateServiceRequest
        {
            ServiceId = _existingService.Id,
            Title = "Only Title Updated",
            // Description, DetailedDescription, etc. are null - should not be updated
            RequestId = "partial-update-123",
            UserContext = "admin@example.com",
            ClientIpAddress = "192.168.1.25",
            UserAgent = "Partial-Update-Agent"
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
        
        // ACT: Execute partial update
        var result = await _useCase.ExecuteAsync(request);
        
        // ASSERT: Should only change provided fields
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        
        // Only title should be tracked as changed
        Assert.True(result.Value.Changes.ContainsKey("Title"));
        Assert.False(result.Value.Changes.ContainsKey("Description"));
        Assert.False(result.Value.Changes.ContainsKey("DetailedDescription"));
        Assert.False(result.Value.Changes.ContainsKey("Available"));
    }

    [Fact(DisplayName = "TDD RED: UpdateService Should Handle Concurrent Updates", Timeout = 5000)]
    public async Task ExecuteAsync_WithConcurrentUpdates_ShouldHandleGracefully()
    {
        // ARRANGE: Multiple concurrent update requests
        var requests = Enumerable.Range(1, 3).Select(i => new UpdateServiceRequest
        {
            ServiceId = _existingService.Id,
            Title = $"Concurrent Update {i}",
            RequestId = $"concurrent-update-{i}",
            UserContext = $"user-{i}@test.com",
            ClientIpAddress = "127.0.0.1",
            UserAgent = "Concurrent-Test-Agent"
        }).ToList();
        
        _mockServiceRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<ServiceId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_existingService);
        
        _mockServiceRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        _mockServiceRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));
        
        // ACT: Execute concurrent updates
        var tasks = requests.Select(r => _useCase.ExecuteAsync(r)).ToArray();
        var results = await Task.WhenAll(tasks);
        
        // ASSERT: All updates should succeed
        Assert.All(results, result => Assert.True(result.IsSuccess));
        Assert.Equal(3, results.Length);
        
        // Verify repository was called for each request
        _mockServiceRepository.Verify(r => r.GetByIdAsync(It.IsAny<ServiceId>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        _mockServiceRepository.Verify(r => r.UpdateAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        _mockServiceRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
    }
}