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
/// TDD RED: Unit tests for CreateServiceUseCase - medical-grade validation and audit logging
/// Tests the Use Case contract without external dependencies (repositories are mocked)
/// Validates medical-grade standards: input validation, error handling, audit trails
/// </summary>
public class CreateServiceUseCaseTests
{
    private readonly Mock<IServiceRepository> _mockServiceRepository;
    private readonly Mock<ILogger<CreateServiceUseCase>> _mockLogger;
    private readonly CreateServiceUseCase _useCase;

    public CreateServiceUseCaseTests()
    {
        _mockServiceRepository = new Mock<IServiceRepository>();
        _mockLogger = new Mock<ILogger<CreateServiceUseCase>>();
        _useCase = new CreateServiceUseCase(_mockServiceRepository.Object, _mockLogger.Object);
    }

    [Fact(DisplayName = "TDD RED: CreateService Should Fail With Null Request")]
    public async Task ExecuteAsync_WithNullRequest_ShouldReturnValidationFailure()
    {
        // ARRANGE: Null request (contract violation)
        CreateServiceRequest? nullRequest = null;
        
        // ACT: Execute use case with invalid input
        var result = await _useCase.ExecuteAsync(nullRequest!);
        
        // ASSERT: Medical-grade validation should catch null input
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("VALIDATION_ERROR", result.Error.Code);
        Assert.Contains("request cannot be null", result.Error.Message.ToLowerInvariant());
    }

    [Fact(DisplayName = "TDD RED: CreateService Should Fail With Empty Title")]
    public async Task ExecuteAsync_WithEmptyTitle_ShouldReturnValidationFailure()
    {
        // ARRANGE: Request with empty title (business rule violation)
        var request = new CreateServiceRequest
        {
            Title = "", // Invalid
            Description = "Valid description",
            Slug = "valid-slug",
            RequestId = "test-request-id",
            UserContext = "admin-user",
            ClientIpAddress = "127.0.0.1",
            UserAgent = "Test-Agent"
        };
        
        // ACT: Execute use case with invalid business data
        var result = await _useCase.ExecuteAsync(request);
        
        // ASSERT: Business validation should fail
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("VALIDATION_ERROR", result.Error.Code);
        Assert.Contains("title", result.Error.Message.ToLowerInvariant());
    }

    [Fact(DisplayName = "TDD RED: CreateService Should Fail With Null Slug")]
    public async Task ExecuteAsync_WithNullSlug_ShouldReturnValidationFailure()
    {
        // ARRANGE: Request with null slug (business rule violation)
        var request = new CreateServiceRequest
        {
            Title = "Valid Title",
            Description = "Valid description",
            Slug = null!, // Invalid
            RequestId = "test-request-id",
            UserContext = "admin-user",
            ClientIpAddress = "127.0.0.1",
            UserAgent = "Test-Agent"
        };
        
        // ACT: Execute use case with invalid slug
        var result = await _useCase.ExecuteAsync(request);
        
        // ASSERT: Slug validation should fail
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("VALIDATION_ERROR", result.Error.Code);
        Assert.Contains("slug", result.Error.Message.ToLowerInvariant());
    }

    [Fact(DisplayName = "TDD RED: CreateService Should Fail With Missing Audit Context")]
    public async Task ExecuteAsync_WithMissingAuditContext_ShouldReturnValidationFailure()
    {
        // ARRANGE: Request missing medical-grade audit context
        var request = new CreateServiceRequest
        {
            Title = "Valid Title",
            Description = "Valid description",
            Slug = "valid-slug",
            RequestId = null!, // Missing audit context
            UserContext = "admin-user",
            ClientIpAddress = "127.0.0.1",
            UserAgent = "Test-Agent"
        };
        
        // ACT: Execute use case with missing audit data
        var result = await _useCase.ExecuteAsync(request);
        
        // ASSERT: Medical-grade audit validation should fail
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("VALIDATION_ERROR", result.Error.Code);
        Assert.Contains("requestid", result.Error.Message.ToLowerInvariant());
    }

    [Fact(DisplayName = "TDD RED: CreateService Should Fail When Repository Save Fails")]
    public async Task ExecuteAsync_WhenRepositoryFails_ShouldReturnRepositoryFailure()
    {
        // ARRANGE: Valid request but repository failure
        var request = new CreateServiceRequest
        {
            Title = "Valid Title",
            Description = "Valid description",
            Slug = "valid-slug",
            RequestId = "test-request-id",
            UserContext = "admin-user",
            ClientIpAddress = "127.0.0.1",
            UserAgent = "Test-Agent"
        };
        
        _mockServiceRepository
            .Setup(r => r.AddAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));
        
        // ACT: Execute use case with repository failure
        var result = await _useCase.ExecuteAsync(request);
        
        // ASSERT: Should handle repository errors gracefully
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("REPOSITORY_ERROR", result.Error.Code);
        Assert.Contains("database", result.Error.Message.ToLowerInvariant());
    }

    [Fact(DisplayName = "TDD RED: CreateService Should Succeed With Valid Request")]
    public async Task ExecuteAsync_WithValidRequest_ShouldReturnSuccess()
    {
        // ARRANGE: Fully valid request with medical-grade audit context
        var request = new CreateServiceRequest
        {
            Title = "Test Service Title",
            Description = "Test service description",
            DetailedDescription = "Detailed test description",
            Slug = "test-service-slug",
            RequestId = "test-request-id-123",
            UserContext = "admin@example.com",
            ClientIpAddress = "192.168.1.100",
            UserAgent = "Mozilla/5.0 Test Agent"
        };
        
        _mockServiceRepository
            .Setup(r => r.AddAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        _mockServiceRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));
        
        // ACT: Execute use case with valid input
        var result = await _useCase.ExecuteAsync(request);
        
        // ASSERT: Should succeed with medical-grade audit logging
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotNull(result.Value.ServiceId);
        
        // Verify audit logging occurred
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CREATE_SERVICE_SUCCESS")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        
        // Verify repository interaction
        _mockServiceRepository.Verify(r => r.AddAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockServiceRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "TDD RED: CreateService Should Log Audit Trail For All Operations")]
    public async Task ExecuteAsync_ShouldLogMedicalGradeAuditTrail()
    {
        // ARRANGE: Request that will trigger audit logging
        var request = new CreateServiceRequest
        {
            Title = "Audit Test Service",
            Description = "Service for testing audit trails",
            Slug = "audit-test-service",
            RequestId = "audit-request-123",
            UserContext = "audit-admin@test.com",
            ClientIpAddress = "10.0.0.1",
            UserAgent = "Audit-Test-Agent/1.0"
        };
        
        _mockServiceRepository
            .Setup(r => r.AddAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()))
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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CREATE_SERVICE_STARTED")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        
        // Verify audit success logging
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CREATE_SERVICE_SUCCESS")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact(DisplayName = "TDD RED: CreateService Should Handle Concurrent Access Gracefully")]
    public async Task ExecuteAsync_WithConcurrentAccess_ShouldHandleGracefully()
    {
        // ARRANGE: Multiple concurrent requests (stress testing)
        var requests = Enumerable.Range(1, 5).Select(i => new CreateServiceRequest
        {
            Title = $"Concurrent Service {i}",
            Description = $"Description {i}",
            Slug = $"concurrent-service-{i}",
            RequestId = $"concurrent-request-{i}",
            UserContext = $"user-{i}@test.com",
            ClientIpAddress = "127.0.0.1",
            UserAgent = "Concurrent-Test-Agent"
        }).ToList();
        
        _mockServiceRepository
            .Setup(r => r.AddAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        _mockServiceRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));
        
        // ACT: Execute multiple concurrent requests
        var tasks = requests.Select(r => _useCase.ExecuteAsync(r)).ToArray();
        var results = await Task.WhenAll(tasks);
        
        // ASSERT: All requests should succeed
        Assert.All(results, result => Assert.True(result.IsSuccess));
        Assert.Equal(5, results.Length);
        
        // Verify repository was called for each request
        _mockServiceRepository.Verify(r => r.AddAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
        _mockServiceRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(5));
    }
}