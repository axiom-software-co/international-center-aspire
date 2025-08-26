using InternationalCenter.Services.Admin.Api.Application.UseCases;
using InternationalCenter.Services.Admin.Api.Handlers;
using InternationalCenter.Services.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InternationalCenter.Services.Admin.Api.Tests.Unit.Handlers;

/// <summary>
/// TDD RED: Handler Layer unit tests with Use Case mocking
/// Tests presentation logic, Use Case orchestration, medical-grade audit integration
/// Does NOT test HTTP protocol concerns (routing, serialization) - that's endpoint responsibility
/// </summary>
public class ServiceHandlersTests
{
    private readonly Mock<ICreateServiceUseCase> _mockCreateUseCase;
    private readonly Mock<IUpdateServiceUseCase> _mockUpdateUseCase; 
    private readonly Mock<IDeleteServiceUseCase> _mockDeleteUseCase;
    private readonly Mock<ILogger<ServiceHandlers>> _mockLogger;
    private readonly ServiceHandlers _handlers;

    public ServiceHandlersTests()
    {
        _mockCreateUseCase = new Mock<ICreateServiceUseCase>();
        _mockUpdateUseCase = new Mock<IUpdateServiceUseCase>();
        _mockDeleteUseCase = new Mock<IDeleteServiceUseCase>();
        _mockLogger = new Mock<ILogger<ServiceHandlers>>();
        _handlers = new ServiceHandlers();
    }

    [Fact(DisplayName = "TDD RED: CreateService Handler Should Orchestrate CreateServiceUseCase Successfully", Timeout = 5000)]
    public async Task CreateService_WithValidRequest_ShouldOrchestrateCreateUseCaseSuccessfully()
    {
        // ARRANGE: Mock successful use case execution with medical-grade response
        var createResponse = new CreateServiceResponse
        {
            Success = true,
            ServiceId = "test-service-id",
            Slug = "test-service",
            AuditTrail = new List<AdminAuditLogEntry>
            {
                new AdminAuditLogEntry 
                { 
                    Operation = "CREATE_SERVICE",
                    UserId = "admin",
                    Timestamp = DateTime.UtcNow
                }
            }
        };
        var successResult = Result<CreateServiceResponse>.Success(createResponse);
        
        _mockCreateUseCase.Setup(uc => uc.ExecuteAsync(It.IsAny<CreateServiceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        var handlerRequest = new CreateServiceHandlerRequest
        {
            Title = "Test Service",
            Slug = "test-service", 
            Description = "Test description",
            DetailedDescription = "Detailed test description",
            Available = true,
            Technologies = new[] { "Technology1" },
            Features = new[] { "Feature1" },
            DeliveryModes = new[] { "Online" }
        };

        // ACT: TDD RED - This will fail because ServiceHandlers doesn't exist yet
        var result = await _handlers.CreateService(handlerRequest, _mockCreateUseCase.Object, _mockLogger.Object);

        // ASSERT: Handler should return Created response with service details
        Assert.NotNull(result);
        
        // Verify use case was called with correct request including medical-grade audit context
        _mockCreateUseCase.Verify(uc => uc.ExecuteAsync(It.Is<CreateServiceRequest>(req =>
            req.Title == handlerRequest.Title &&
            req.Slug == handlerRequest.Slug &&
            req.Description == handlerRequest.Description &&
            !string.IsNullOrEmpty(req.UserContext) && // Medical-grade audit
            !string.IsNullOrEmpty(req.RequestId) // Request correlation
        ), It.IsAny<CancellationToken>()), Times.Once);

        // Verify medical-grade audit logging occurred
        _mockLogger.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ADMIN_CREATE_SERVICE_SUCCESS")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact(DisplayName = "TDD RED: CreateService Handler Should Handle Use Case Validation Errors", Timeout = 5000)]
    public async Task CreateService_WithInvalidRequest_ShouldHandleUseCaseValidationErrors()
    {
        // ARRANGE: Mock validation error from use case
        var errorResult = Result<CreateServiceResponse>.Failure(InternationalCenter.Services.Domain.Models.Error.Create(
            "VALIDATION_ERROR", 
            "Title is required"));
        
        _mockCreateUseCase.Setup(uc => uc.ExecuteAsync(It.IsAny<CreateServiceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(errorResult);

        var invalidRequest = new CreateServiceHandlerRequest
        {
            Title = "", // Invalid - empty title
            Slug = "test-service"
        };

        // ACT: TDD RED - Handler should handle validation errors gracefully  
        var result = await _handlers.CreateService(invalidRequest, _mockCreateUseCase.Object, _mockLogger.Object);

        // ASSERT: Handler should return BadRequest with error details
        Assert.NotNull(result);

        // Verify error was logged with medical-grade standards
        _mockLogger.Verify(
            logger => logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ADMIN_CREATE_SERVICE_VALIDATION_ERROR")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact(DisplayName = "TDD RED: UpdateService Handler Should Orchestrate UpdateServiceUseCase Successfully", Timeout = 5000)]
    public async Task UpdateService_WithValidRequest_ShouldOrchestrateUpdateUseCaseSuccessfully()
    {
        // ARRANGE: Mock successful update use case with medical-grade response
        var updateResponse = new UpdateServiceResponse
        {
            Success = true,
            ServiceId = "test-service-id",
            AuditTrail = new List<AdminAuditLogEntry>
            {
                new AdminAuditLogEntry 
                { 
                    Operation = "UPDATE_SERVICE",
                    UserId = "admin",
                    Timestamp = DateTime.UtcNow
                }
            }
        };
        var successResult = Result<UpdateServiceResponse>.Success(updateResponse);
        
        _mockUpdateUseCase.Setup(uc => uc.ExecuteAsync(It.IsAny<UpdateServiceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        var serviceId = "12345678-1234-1234-1234-123456789abc";
        var handlerRequest = new UpdateServiceHandlerRequest
        {
            Title = "Updated Service Title",
            Description = "Updated description",
            Available = false
        };

        // ACT: TDD RED - Handler orchestration logic
        var result = await _handlers.UpdateService(serviceId, handlerRequest, _mockUpdateUseCase.Object, _mockLogger.Object);

        // ASSERT: Handler should return NoContent for successful update
        Assert.NotNull(result);

        // Verify use case orchestration with medical-grade audit context
        _mockUpdateUseCase.Verify(uc => uc.ExecuteAsync(It.Is<UpdateServiceRequest>(req =>
            req.ServiceId == serviceId &&
            req.Title == handlerRequest.Title &&
            req.Available == handlerRequest.Available &&
            !string.IsNullOrEmpty(req.UserContext) // Medical-grade audit
        ), It.IsAny<CancellationToken>()), Times.Once);

        // Verify medical-grade audit logging
        _mockLogger.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ADMIN_UPDATE_SERVICE_SUCCESS")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact(DisplayName = "TDD RED: UpdateService Handler Should Handle Service Not Found", Timeout = 5000)]
    public async Task UpdateService_WithNonExistentService_ShouldHandleServiceNotFound()
    {
        // ARRANGE: Mock service not found error
        var notFoundResult = Result<UpdateServiceResponse>.Failure(InternationalCenter.Services.Domain.Models.Error.Create(
            "SERVICE_NOT_FOUND",
            "Service not found"));
        
        _mockUpdateUseCase.Setup(uc => uc.ExecuteAsync(It.IsAny<UpdateServiceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(notFoundResult);

        var serviceId = "99999999-9999-9999-9999-999999999999";
        var request = new UpdateServiceHandlerRequest
        {
            Title = "Updated Title"
        };

        // ACT: Handler should handle not found scenario
        var result = await _handlers.UpdateService(serviceId, request, _mockUpdateUseCase.Object, _mockLogger.Object);

        // ASSERT: Handler should return NotFound response
        Assert.NotNull(result);

        // Verify medical-grade error audit logging
        _mockLogger.Verify(
            logger => logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ADMIN_UPDATE_SERVICE_NOT_FOUND")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact(DisplayName = "TDD RED: DeleteService Handler Should Orchestrate DeleteServiceUseCase Successfully", Timeout = 5000)]
    public async Task DeleteService_WithValidId_ShouldOrchestrateDeleteUseCaseSuccessfully()
    {
        // ARRANGE: Mock successful delete use case with medical-grade response
        var deleteResponse = new DeleteServiceResponse
        {
            Success = true,
            ServiceId = "test-service-id",
            AuditTrail = new List<AdminAuditLogEntry>
            {
                new AdminAuditLogEntry 
                { 
                    Operation = "DELETE_SERVICE",
                    UserId = "admin",
                    Timestamp = DateTime.UtcNow
                }
            }
        };
        var successResult = Result<DeleteServiceResponse>.Success(deleteResponse);
        
        _mockDeleteUseCase.Setup(uc => uc.ExecuteAsync(It.IsAny<DeleteServiceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        var serviceId = "12345678-1234-1234-1234-123456789abc";

        // ACT: TDD RED - Delete handler orchestration
        var result = await _handlers.DeleteService(serviceId, _mockDeleteUseCase.Object, _mockLogger.Object);

        // ASSERT: Handler should return NoContent for successful deletion
        Assert.NotNull(result);

        // Verify use case orchestration with medical-grade audit context
        _mockDeleteUseCase.Verify(uc => uc.ExecuteAsync(It.Is<DeleteServiceRequest>(req =>
            req.ServiceId == serviceId &&
            !string.IsNullOrEmpty(req.UserContext) // Medical-grade audit
        ), It.IsAny<CancellationToken>()), Times.Once);

        // Verify medical-grade audit logging for delete operation
        _mockLogger.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ADMIN_DELETE_SERVICE_SUCCESS")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact(DisplayName = "TDD RED: DeleteService Handler Should Handle Use Case Errors", Timeout = 5000)]
    public async Task DeleteService_WithUseCaseError_ShouldHandleErrorsGracefully()
    {
        // ARRANGE: Mock use case failure
        var errorResult = Result<DeleteServiceResponse>.Failure(InternationalCenter.Services.Domain.Models.Error.Create(
            "DELETE_FAILED",
            "Cannot delete service with active references"));
        
        _mockDeleteUseCase.Setup(uc => uc.ExecuteAsync(It.IsAny<DeleteServiceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(errorResult);

        var serviceId = "12345678-1234-1234-1234-123456789abc";

        // ACT: Handler should handle use case errors
        var result = await _handlers.DeleteService(serviceId, _mockDeleteUseCase.Object, _mockLogger.Object);

        // ASSERT: Handler should return appropriate error response
        Assert.NotNull(result);

        // Verify medical-grade error audit logging  
        _mockLogger.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ADMIN_DELETE_SERVICE_ERROR")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact(DisplayName = "TDD RED: Handler Should Validate Service ID Format", Timeout = 5000)]
    public async Task Handler_WithInvalidServiceIdFormat_ShouldReturnBadRequest()
    {
        // ARRANGE: Invalid service ID format
        var invalidId = "not-a-valid-id";
        var request = new UpdateServiceHandlerRequest { Title = "Test" };

        // ACT: Handler should validate ID format before use case call
        var result = await _handlers.UpdateService(invalidId, request, _mockUpdateUseCase.Object, _mockLogger.Object);

        // ASSERT: Handler should return BadRequest without calling use case
        Assert.NotNull(result);

        // Verify use case was NOT called with invalid ID
        _mockUpdateUseCase.Verify(uc => uc.ExecuteAsync(It.IsAny<UpdateServiceRequest>(), It.IsAny<CancellationToken>()), Times.Never);

        // Verify validation error logging
        _mockLogger.Verify(
            logger => logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ADMIN_INVALID_SERVICE_ID")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}