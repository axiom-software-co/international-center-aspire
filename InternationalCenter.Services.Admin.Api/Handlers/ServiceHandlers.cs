using InternationalCenter.Services.Admin.Api.Application.UseCases;
using InternationalCenter.Services.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace InternationalCenter.Services.Admin.Api.Handlers;

/// <summary>
/// TDD REFACTOR: Service handlers for orchestrating Use Cases with medical-grade audit logging
/// Optimized for maintainability, performance, and medical-grade standards
/// Handles presentation logic, Use Case coordination, and HTTP response formatting
/// Does NOT handle HTTP routing/serialization - that's endpoint layer responsibility
/// </summary>
public class ServiceHandlers
{
    /// <summary>
    /// TDD REFACTOR: Create service handler orchestrating CreateServiceUseCase with optimized audit patterns
    /// </summary>
    public async Task<IResult> CreateService(
        CreateServiceHandlerRequest request,
        ICreateServiceUseCase useCase,
        ILogger<ServiceHandlers> logger)
    {
        return await ExecuteWithMedicalGradeAudit(
            "CREATE_SERVICE",
            request.Title,
            logger,
            async (requestId) =>
            {
                var useCaseRequest = new CreateServiceRequest
                {
                    Title = request.Title,
                    Slug = request.Slug,
                    Description = request.Description,
                    DetailedDescription = request.DetailedDescription,
                    Available = request.Available,
                    Technologies = request.Technologies,
                    Features = request.Features,
                    DeliveryModes = request.DeliveryModes,
                    Icon = request.Icon,
                    Image = request.Image,
                    MetaTitle = request.MetaTitle,
                    MetaDescription = request.MetaDescription,
                    
                    // Medical-grade audit context
                    UserContext = GetAuditUserContext(),
                    RequestId = requestId,
                    ClientIpAddress = GetAuditClientIp(),
                    UserAgent = GetAuditUserAgent()
                };

                var result = await useCase.ExecuteAsync(useCaseRequest);

                if (result.IsSuccess)
                {
                    return Results.Created($"/api/admin/services/{result.Value.ServiceId}", new
                    {
                        id = result.Value.ServiceId,
                        slug = result.Value.Slug,
                        success = true,
                        auditTrail = result.Value.AuditTrail,
                        performanceMetrics = result.Value.PerformanceMetrics
                    });
                }

                return FormatErrorResponse(result.Error, requestId);
            });
    }

    /// <summary>
    /// TDD REFACTOR: Update service handler orchestrating UpdateServiceUseCase with optimized audit patterns
    /// </summary>
    public async Task<IResult> UpdateService(
        string serviceId,
        UpdateServiceHandlerRequest request,
        IUpdateServiceUseCase useCase,
        ILogger<ServiceHandlers> logger)
    {
        // Validate service ID format before use case call
        var validationResult = ValidateServiceId(serviceId, logger);
        if (validationResult != null)
            return validationResult;

        return await ExecuteWithMedicalGradeAudit(
            "UPDATE_SERVICE",
            serviceId,
            logger,
            async (requestId) =>
            {
                var useCaseRequest = new UpdateServiceRequest
                {
                    ServiceId = serviceId,
                    Title = request.Title,
                    Description = request.Description,
                    DetailedDescription = request.DetailedDescription,
                    Available = request.Available,
                    Technologies = request.Technologies,
                    Features = request.Features,
                    DeliveryModes = request.DeliveryModes,
                    Icon = request.Icon,
                    Image = request.Image,
                    MetaTitle = request.MetaTitle,
                    MetaDescription = request.MetaDescription,
                    
                    // Medical-grade audit context
                    UserContext = GetAuditUserContext(),
                    RequestId = requestId,
                    ClientIpAddress = GetAuditClientIp(),
                    UserAgent = GetAuditUserAgent()
                };

                var result = await useCase.ExecuteAsync(useCaseRequest);

                if (result.IsSuccess)
                {
                    return Results.NoContent();
                }

                return FormatErrorResponse(result.Error, requestId);
            });
    }

    /// <summary>
    /// TDD REFACTOR: Delete service handler orchestrating DeleteServiceUseCase with optimized audit patterns
    /// </summary>
    public async Task<IResult> DeleteService(
        string serviceId,
        IDeleteServiceUseCase useCase,
        ILogger<ServiceHandlers> logger)
    {
        return await ExecuteWithMedicalGradeAudit(
            "DELETE_SERVICE",
            serviceId,
            logger,
            async (requestId) =>
            {
                var useCaseRequest = new DeleteServiceRequest
                {
                    ServiceId = serviceId,
                    DeletionReason = "Admin deletion request", // Required for use case validation
                    
                    // Medical-grade audit context
                    UserContext = GetAuditUserContext(),
                    RequestId = requestId,
                    ClientIpAddress = GetAuditClientIp(),
                    UserAgent = GetAuditUserAgent()
                };

                var result = await useCase.ExecuteAsync(useCaseRequest);

                if (result.IsSuccess)
                {
                    return Results.NoContent();
                }

                return FormatErrorResponse(result.Error, requestId);
            });
    }

    /// <summary>
    /// TDD REFACTOR: Common execution pattern with medical-grade audit logging and error handling
    /// </summary>
    private static async Task<IResult> ExecuteWithMedicalGradeAudit(
        string operation,
        string entityIdentifier,
        ILogger<ServiceHandlers> logger,
        Func<string, Task<IResult>> operationFunc)
    {
        try
        {
            var requestId = GenerateRequestId();
            
            // Medical-grade audit: Log request initiation
            logger.LogInformation("ADMIN_{Operation}_INITIATED: RequestId={RequestId}, Identifier={Identifier}", 
                operation, requestId, entityIdentifier);

            var result = await operationFunc(requestId);

            // Medical-grade audit: Log successful completion
            logger.LogInformation("ADMIN_{Operation}_SUCCESS: RequestId={RequestId}, Identifier={Identifier}", 
                operation, requestId, entityIdentifier);

            return result;
        }
        catch (Exception ex)
        {
            // Medical-grade audit: Log unexpected errors
            logger.LogError(ex, "ADMIN_{Operation}_EXCEPTION: Identifier={Identifier}", 
                operation, entityIdentifier);
            
            return Results.Problem(
                title: "Internal Server Error",
                detail: $"An error occurred while processing {operation.ToLowerInvariant().Replace('_', ' ')}",
                statusCode: 500);
        }
    }

    /// <summary>
    /// Validates service ID format and returns appropriate error response if invalid
    /// </summary>
    private static IResult? ValidateServiceId(string serviceId, ILogger<ServiceHandlers> logger)
    {
        if (string.IsNullOrEmpty(serviceId) || !IsValidServiceIdFormat(serviceId))
        {
            logger.LogWarning("ADMIN_INVALID_SERVICE_ID: ServiceId={ServiceId}", serviceId);
            return Results.BadRequest(new
            {
                error = "INVALID_SERVICE_ID",
                message = "Service ID format is invalid"
            });
        }
        return null;
    }

    /// <summary>
    /// Validates service ID format - must be a valid GUID format for entity identification
    /// </summary>
    private static bool IsValidServiceIdFormat(string serviceId)
    {
        if (string.IsNullOrEmpty(serviceId))
            return false;
            
        // Must be a valid GUID format (with or without dashes)
        return Guid.TryParse(serviceId, out _);
    }

    /// <summary>
    /// Formats error responses with consistent structure and medical-grade audit context
    /// </summary>
    private static IResult FormatErrorResponse(DomainError? error, string requestId)
    {
        var errorResponse = new
        {
            error = error?.Code,
            message = error?.Message,
            requestId = requestId
        };

        return error?.Code switch
        {
            "SERVICE_NOT_FOUND" => Results.NotFound(errorResponse),
            _ => Results.BadRequest(errorResponse)
        };
    }

    /// <summary>
    /// Generates unique request ID for medical-grade audit tracking
    /// </summary>
    private static string GenerateRequestId() => Guid.NewGuid().ToString();

    /// <summary>
    /// Gets audit user context - TODO: Replace with actual HTTP context extraction
    /// </summary>
    private static string GetAuditUserContext() => "admin";

    /// <summary>
    /// Gets audit client IP - TODO: Replace with actual HTTP context extraction
    /// </summary>
    private static string GetAuditClientIp() => "127.0.0.1";

    /// <summary>
    /// Gets audit user agent - TODO: Replace with actual HTTP context extraction
    /// </summary>
    private static string GetAuditUserAgent() => "Admin-API";
}

/// <summary>
/// TDD GREEN: Handler-specific Request DTOs for HTTP layer
/// These are mapped to Use Case requests by handlers
/// </summary>
public record CreateServiceHandlerRequest
{
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string DetailedDescription { get; init; } = string.Empty;
    public bool Available { get; init; } = true;
    public string[]? Technologies { get; init; }
    public string[]? Features { get; init; }
    public string[]? DeliveryModes { get; init; }
    public string? Icon { get; init; }
    public string? Image { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
}

public record UpdateServiceHandlerRequest
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? DetailedDescription { get; init; }
    public bool? Available { get; init; }
    public string[]? Technologies { get; init; }
    public string[]? Features { get; init; }
    public string[]? DeliveryModes { get; init; }
    public string? Icon { get; init; }
    public string? Image { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
}