using InternationalCenter.Services.Admin.Api.Application.UseCases;
using InternationalCenter.Services.Domain.Entities;
using InternationalCenter.Services.Domain.Models;
using InternationalCenter.Services.Domain.Repositories;
using InternationalCenter.Services.Domain.ValueObjects;
using InternationalCenter.Tests.Shared.Base;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace InternationalCenter.Services.Admin.Api.Tests.Unit.UseCases;

/// <summary>
/// Unit tests for CreateServiceUseCase - contract-first TDD with mocked dependencies
/// Tests use case logic in isolation using mocks for all external dependencies
/// Validates business rules, validation logic, and error handling contracts
/// Integration tests with real dependencies are in separate Integration test project
/// </summary>
public class CreateServiceUseCaseTests : UnitTestBase
{
    private readonly Mock<IServiceRepository> _mockServiceRepository;
    private readonly Mock<IServiceCategoryRepository> _mockCategoryRepository;
    private readonly Mock<ILogger<CreateServiceUseCase>> _mockLogger;
    private readonly CreateServiceUseCase _useCase;

    public CreateServiceUseCaseTests(ITestOutputHelper output) : base(output)
    {
        _mockServiceRepository = new Mock<IServiceRepository>();
        _mockCategoryRepository = new Mock<IServiceCategoryRepository>();
        _mockLogger = CreateMockLogger<CreateServiceUseCase>();
        
        // Create use case with mocked dependencies - this is proper unit testing
        _useCase = new CreateServiceUseCase(_mockServiceRepository.Object, _mockLogger.Object);
    }

    private CreateServiceRequest CreateValidRequest()
    {
        return new CreateServiceRequest
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
    }

    private CreateServiceRequest[] CreateInvalidRequests()
    {
        return new[]
        {
            new CreateServiceRequest
            {
                Title = "", // Invalid empty title
                Description = "Valid description",
                Slug = "valid-slug",
                RequestId = "test-request-id",
                UserContext = "admin-user",
                ClientIpAddress = "127.0.0.1",
                UserAgent = "Test-Agent"
            },
            new CreateServiceRequest
            {
                Title = "Valid Title",
                Description = "Valid description",
                Slug = null!, // Invalid null slug
                RequestId = "test-request-id",
                UserContext = "admin-user",
                ClientIpAddress = "127.0.0.1",
                UserAgent = "Test-Agent"
            },
            new CreateServiceRequest
            {
                Title = "Valid Title",
                Description = "Valid description",
                Slug = "valid-slug",
                RequestId = null!, // Missing audit context
                UserContext = "admin-user",
                ClientIpAddress = "127.0.0.1",
                UserAgent = "Test-Agent"
            }
        };
    }

    private async Task<Result<CreateServiceResponse>> ExecuteUseCaseAsync(CreateServiceRequest request, CancellationToken cancellationToken = default)
    {
        return await _useCase.ExecuteAsync(request, cancellationToken);
    }

    [Fact(Timeout = 5000)]
    public async Task ExecuteAsync_WithNullRequest_ReturnsValidationError()
    {
        // Arrange
        CreateServiceRequest? nullRequest = null;

        // Act
        var result = await ExecuteUseCaseAsync(nullRequest!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Contains("VALIDATION", result.Error.Code);
        Assert.Contains("request", result.Error.Message.ToLowerInvariant());

        Output.WriteLine("✅ NULL REQUEST CONTRACT: Null request properly rejected with validation error");
    }

    [Fact(Timeout = 5000)]
    public async Task ExecuteAsync_WithInvalidRequests_ReturnsValidationErrors()
    {
        // Arrange
        var invalidRequests = CreateInvalidRequests();

        foreach (var invalidRequest in invalidRequests)
        {
            // Act
            var result = await ExecuteUseCaseAsync(invalidRequest);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.True(result.Error.Code == "VALIDATION_ERROR" || result.Error.Code.Contains("VALIDATION"));

            Output.WriteLine($"✅ VALIDATION CONTRACT: Invalid request {invalidRequest.GetType().Name} properly rejected");
        }
    }

    [Fact(Timeout = 5000)]
    public async Task ExecuteAsync_WithEmptyDescription_ReturnsValidationError()
    {
        // Arrange - Test business rule: Services must have non-empty description
        var requestWithEmptyDescription = new CreateServiceRequest
        {
            Title = "Valid Title",
            Description = "", // Business rule violation
            Slug = "valid-slug",
            RequestId = "test-request-id",
            UserContext = "admin-user",
            ClientIpAddress = "127.0.0.1",
            UserAgent = "Test-Agent"
        };

        // Act
        var result = await ExecuteUseCaseAsync(requestWithEmptyDescription);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Contains("VALIDATION", result.Error.Code);

        Output.WriteLine("✅ BUSINESS RULE CONTRACT: Empty description properly rejected");
    }

    [Fact(Timeout = 5000)]
    public async Task ExecuteAsync_WithRepositoryFailure_ReturnsInfrastructureError()
    {
        // Arrange - Mock repository to throw exception
        _mockServiceRepository
            .Setup(r => r.AddAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var validRequest = CreateValidRequest();

        // Act
        var result = await ExecuteUseCaseAsync(validRequest);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.True(result.Error.Code.Contains("INFRASTRUCTURE") || result.Error.Code.Contains("REPOSITORY"));

        // Verify error message doesn't expose internal implementation details
        Assert.DoesNotContain("Database connection failed", result.Error.Message ?? "");

        Output.WriteLine("✅ INFRASTRUCTURE CONTRACT: Repository failures handled gracefully");
    }

    [Fact(Timeout = 5000)]
    public async Task ExecuteAsync_WithValidRequest_ReturnsSuccessResult()
    {
        // Arrange
        SetupSuccessfulRepositoryMocks();
        var validRequest = CreateValidRequest();

        // Act
        var result = await ExecuteUseCaseAsync(validRequest);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotNull(result.Value.ServiceId);

        Output.WriteLine("✅ SUCCESS CONTRACT: Valid request processed successfully");
    }

    private void SetupSuccessfulRepositoryMocks()
    {
        _mockServiceRepository
            .Setup(r => r.AddAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockServiceRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    [Fact(Timeout = 5000)]
    public async Task ExecuteAsync_WithCancelledToken_ThrowsOperationCancelledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var validRequest = CreateValidRequest();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => 
            await ExecuteUseCaseAsync(validRequest, cts.Token));

        Output.WriteLine("✅ CANCELLATION CONTRACT: Cancellation tokens properly respected");
    }

    [Fact(DisplayName = "CreateService Should Validate Service Title Length", Timeout = 5000)]
    public async Task ExecuteAsync_WithTooLongTitle_ShouldReturnValidationFailure()
    {
        // ARRANGE: Request with title exceeding business limits
        var requestWithLongTitle = new CreateServiceRequest
        {
            Title = new string('A', 501), // Assuming 500 char limit
            Description = "Valid description",
            Slug = "valid-slug",
            RequestId = "test-request-id",
            UserContext = "admin-user",
            ClientIpAddress = "127.0.0.1",
            UserAgent = "Test-Agent"
        };
        
        // ACT: Execute use case with invalid input
        var result = await ExecuteUseCaseAsync(requestWithLongTitle);
        
        // ASSERT: Business validation should fail
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("VALIDATION_ERROR", result.Error.Code);
        Assert.Contains("title", result.Error.Message.ToLowerInvariant());
        
        // Verify contract compliance
        ValidateErrorResponseSafety(result);
    }

    [Fact(DisplayName = "CreateService Should Validate Slug Format", Timeout = 5000)]
    public async Task ExecuteAsync_WithInvalidSlugFormat_ShouldReturnValidationFailure()
    {
        // ARRANGE: Request with invalid slug format
        var requestWithInvalidSlug = new CreateServiceRequest
        {
            Title = "Valid Title",
            Description = "Valid description",
            Slug = "Invalid Slug With Spaces!", // Invalid format
            RequestId = "test-request-id",
            UserContext = "admin-user",
            ClientIpAddress = "127.0.0.1",
            UserAgent = "Test-Agent"
        };
        
        // ACT: Execute use case with invalid slug
        var result = await ExecuteUseCaseAsync(requestWithInvalidSlug);
        
        // ASSERT: Validation should fail
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("VALIDATION_ERROR", result.Error.Code);
        Assert.Contains("slug", result.Error.Message.ToLowerInvariant());
        
        // Verify contract compliance
        ValidateErrorResponseSafety(result);
    }

    [Fact(DisplayName = "CreateService Should Handle Duplicate Slug", Timeout = 5000)]
    public async Task ExecuteAsync_WithDuplicateSlug_ShouldReturnBusinessRuleError()
    {
        // ARRANGE: Setup repository to indicate slug already exists
        var existingSlug = Slug.Create("existing-service-slug");
        _mockServiceRepository
            .Setup(r => r.SlugExistsAsync(existingSlug, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        var requestWithDuplicateSlug = new CreateServiceRequest
        {
            Title = "Valid Title",
            Description = "Valid description",
            Slug = "existing-service-slug",
            RequestId = "test-request-id",
            UserContext = "admin-user",
            ClientIpAddress = "127.0.0.1",
            UserAgent = "Test-Agent"
        };
        
        // ACT: Execute use case with duplicate slug
        var result = await ExecuteUseCaseAsync(requestWithDuplicateSlug);
        
        // ASSERT: Business rule validation should fail
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error.Code);
        Assert.Contains("slug", result.Error.Message.ToLowerInvariant());
        Assert.Contains("already exists", result.Error.Message.ToLowerInvariant());
        
        // Verify contract compliance
        ValidateErrorResponseSafety(result);
    }

    [Fact(DisplayName = "CreateService Should Validate Medical-Grade Audit Context", Timeout = 5000)]
    public async Task ExecuteAsync_WithCompleteAuditContext_ShouldPassValidation()
    {
        // ARRANGE: Request with complete medical-grade audit context
        SetupSuccessfulRepositoryMocks();
        var requestWithFullAudit = CreateValidRequest();
        
        // ACT: Execute use case with complete audit context
        var result = await ExecuteUseCaseAsync(requestWithFullAudit);
        
        // ASSERT: Should succeed with proper audit context
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        
        // Verify audit context was properly validated
        ValidateMedicalGradeAuditContext(requestWithFullAudit);
        
        Output.WriteLine("✅ MEDICAL-GRADE AUDIT: Complete audit context properly validated and processed");
    }

    [Fact(DisplayName = "CreateService Should Handle SaveChanges Failure", Timeout = 5000)]
    public async Task ExecuteAsync_WhenSaveChangesFails_ShouldReturnInfrastructureError()
    {
        // ARRANGE: Valid request but SaveChanges fails
        _mockServiceRepository
            .Setup(r => r.AddAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        _mockServiceRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Transaction rollback occurred"));
        
        var validRequest = CreateValidRequest();
        
        // ACT: Execute use case with save failure
        var result = await ExecuteUseCaseAsync(validRequest);
        
        // ASSERT: Should handle save errors gracefully
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("INFRASTRUCTURE_ERROR", result.Error.Code);
        
        // Verify contract compliance - no sensitive information leaked
        ValidateErrorResponseSafety(result);
        Assert.DoesNotContain("transaction rollback", result.Error.Message.ToLowerInvariant());
    }

    [Fact(DisplayName = "CreateService Should Generate Unique Service ID", Timeout = 5000)]
    public async Task ExecuteAsync_WithValidRequest_ShouldGenerateUniqueServiceId()
    {
        // ARRANGE: Multiple valid requests
        SetupSuccessfulRepositoryMocks();
        var request1 = CreateValidRequest();
        var request2 = CreateValidRequest();
        request2.Slug = "different-slug";
        
        // ACT: Execute use case multiple times
        var result1 = await ExecuteUseCaseAsync(request1);
        var result2 = await ExecuteUseCaseAsync(request2);
        
        // ASSERT: Should generate unique service IDs
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.NotNull(result1.Value?.ServiceId);
        Assert.NotNull(result2.Value?.ServiceId);
        Assert.NotEqual(result1.Value.ServiceId, result2.Value.ServiceId);
        
        Output.WriteLine($"✅ UNIQUENESS CONTRACT: Generated unique Service IDs: {result1.Value.ServiceId} and {result2.Value.ServiceId}");
    }

    [Fact(DisplayName = "CreateService Should Set Proper Service Defaults", Timeout = 5000)]
    public async Task ExecuteAsync_WithValidRequest_ShouldSetProperServiceDefaults()
    {
        // ARRANGE
        SetupSuccessfulRepositoryMocks();
        
        // Capture the service entity that gets added to repository
        Service? capturedService = null;
        _mockServiceRepository
            .Setup(r => r.AddAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()))
            .Callback<Service, CancellationToken>((service, token) => capturedService = service)
            .Returns(Task.CompletedTask);
        
        var validRequest = CreateValidRequest();
        
        // ACT: Execute use case
        var result = await ExecuteUseCaseAsync(validRequest);
        
        // ASSERT: Verify service defaults
        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedService);
        
        // Verify business rule: new services should default to Draft status
        Assert.Equal(ServiceStatus.Draft, capturedService.Status);
        Assert.True(capturedService.Available); // Available by default
        Assert.False(capturedService.Featured); // Not featured by default
        Assert.True(capturedService.CreatedAt <= DateTime.UtcNow);
        Assert.True(capturedService.UpdatedAt <= DateTime.UtcNow);
        
        Output.WriteLine("✅ BUSINESS DEFAULTS: Service created with proper default values");
    }
}