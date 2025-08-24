using InternationalCenter.Services.Admin.Api.Application.UseCases;
using InternationalCenter.Services.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;

namespace InternationalCenter.Services.Admin.Api.Infrastructure.Extensions;

/// <summary>
/// Medical-grade Admin REST API endpoints with comprehensive audit trail support
/// Replaces gRPC handlers with minimal APIs following Microsoft patterns
/// </summary>
public static class RestApiExtensions
{
    public static WebApplication MapAdminServicesRestApi(this WebApplication app)
    {
        var adminApiGroup = app.MapGroup("/api/admin")
            .WithTags("Admin Services")
            .WithOpenApi()
            .RequireAuthorization(); // Admin endpoints require authentication
            
        // POST /admin/api/services - Create new service with medical-grade audit
        adminApiGroup.MapPost("/services", async (
            [FromBody] CreateServiceApiRequest request,
            ICreateServiceUseCase useCase,
            ILogger<Program> logger,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            // Medical-grade request context extraction
            var requestContext = ExtractMedicalGradeRequestContext(context, request.RequestId);
            
            // Add observability headers
            AddObservabilityHeaders(context, requestContext.CorrelationId, requestContext.RequestId);
            
            try
            {
                // Map API request to Use Case request
                var useCaseRequest = new CreateServiceRequest
                {
                    Title = request.Title,
                    Slug = request.Slug,
                    Description = request.Description,
                    DetailedDescription = request.DetailedDescription,
                    Technologies = request.Technologies,
                    Features = request.Features,
                    DeliveryModes = request.DeliveryModes,
                    Icon = request.Icon,
                    Image = request.Image,
                    MetaTitle = request.MetaTitle,
                    MetaDescription = request.MetaDescription,
                    Available = request.Available,
                    UserContext = requestContext.UserId,
                    RequestId = requestContext.RequestId,
                    ClientIpAddress = requestContext.IpAddress,
                    UserAgent = requestContext.UserAgent
                };

                var result = await useCase.ExecuteAsync(useCaseRequest, cancellationToken);

                if (result.IsSuccess)
                {
                    logger.LogInformation("ADMIN_AUDIT: Service created successfully - ServiceId: {ServiceId}, UserId: {UserId}", 
                        result.Value.ServiceId, requestContext.UserId);
                    
                    return Results.Created($"/admin/api/services/{result.Value.ServiceId}", new CreateServiceApiResponse
                    {
                        Success = true,
                        ServiceId = result.Value.ServiceId,
                        Title = request.Title,
                        Slug = result.Value.Slug,
                        Status = "Active", // Default status for newly created services
                        Available = request.Available ?? true,
                        AuditTrail = result.Value.AuditTrail?.Select(a => new AuditEntryDto
                        {
                            Operation = a.Operation,
                            OperationType = a.OperationType,
                            UserId = a.UserId,
                            IpAddress = a.IpAddress,
                            UserAgent = a.UserAgent,
                            CorrelationId = a.CorrelationId,
                            Timestamp = a.Timestamp,
                            Changes = a.Changes
                        }).ToList() ?? new List<AuditEntryDto>()
                    });
                }

                logger.LogWarning("ADMIN_AUDIT: Service creation failed - UserId: {UserId}, Error: {Error}", 
                    requestContext.UserId, result.Error.Message);
                
                return Results.BadRequest(new AdminErrorResponse
                {
                    Success = false,
                    Message = result.Error.Message,
                    Code = result.Error.Code,
                    RequestId = requestContext.RequestId
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ADMIN_AUDIT: Service creation failed with exception - UserId: {UserId}", requestContext.UserId);
                return Results.Problem("Internal server error occurred during service creation");
            }
        });

        // PUT /admin/api/services/{id} - Update existing service with medical-grade audit
        adminApiGroup.MapPut("/services/{id}", async (
            string id,
            [FromBody] UpdateServiceApiRequest request,
            IUpdateServiceUseCase useCase,
            ILogger<Program> logger,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var requestContext = ExtractMedicalGradeRequestContext(context, request.RequestId);
            AddObservabilityHeaders(context, requestContext.CorrelationId, requestContext.RequestId);

            try
            {
                var useCaseRequest = new UpdateServiceRequest
                {
                    ServiceId = id,
                    Title = request.Title,
                    Description = request.Description,
                    DetailedDescription = request.DetailedDescription,
                    Technologies = request.Technologies,
                    Features = request.Features,
                    DeliveryModes = request.DeliveryModes,
                    Icon = request.Icon,
                    Image = request.Image,
                    MetaTitle = request.MetaTitle,
                    MetaDescription = request.MetaDescription,
                    Available = request.Available,
                    UserContext = requestContext.UserId,
                    RequestId = requestContext.RequestId,
                    ClientIpAddress = requestContext.IpAddress,
                    UserAgent = requestContext.UserAgent
                };

                var result = await useCase.ExecuteAsync(useCaseRequest, cancellationToken);

                if (result.IsSuccess)
                {
                    logger.LogInformation("ADMIN_AUDIT: Service updated successfully - ServiceId: {ServiceId}, UserId: {UserId}", 
                        id, requestContext.UserId);
                    
                    return Results.Ok(new UpdateServiceApiResponse
                    {
                        Success = true,
                        ServiceId = result.Value.ServiceId,
                        Changes = result.Value.Changes,
                        AuditTrail = result.Value.AuditTrail,
                        PerformanceMetrics = result.Value.PerformanceMetrics
                    });
                }

                if (result.Error.Code == "SERVICE_NOT_FOUND")
                {
                    return Results.NotFound(new AdminErrorResponse
                    {
                        Success = false,
                        Message = result.Error.Message,
                        Code = result.Error.Code,
                        RequestId = requestContext.RequestId
                    });
                }

                return Results.BadRequest(new AdminErrorResponse
                {
                    Success = false,
                    Message = result.Error.Message,
                    Code = result.Error.Code,
                    RequestId = requestContext.RequestId
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ADMIN_AUDIT: Service update failed with exception - ServiceId: {ServiceId}, UserId: {UserId}", 
                    id, requestContext.UserId);
                return Results.Problem("Internal server error occurred during service update");
            }
        });

        // DELETE /admin/api/services/{id} - Soft delete service with medical-grade audit
        adminApiGroup.MapDelete("/services/{id}", async (
            string id,
            [FromBody] DeleteServiceApiRequest request,
            IDeleteServiceUseCase useCase,
            ILogger<Program> logger,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var requestContext = ExtractMedicalGradeRequestContext(context, request.RequestId);
            AddObservabilityHeaders(context, requestContext.CorrelationId, requestContext.RequestId);

            try
            {
                var useCaseRequest = new DeleteServiceRequest
                {
                    ServiceId = id,
                    DeletionReason = request.DeletionReason,
                    ForceDelete = request.ForceDelete,
                    UserContext = requestContext.UserId,
                    RequestId = requestContext.RequestId,
                    ClientIpAddress = requestContext.IpAddress,
                    UserAgent = requestContext.UserAgent
                };

                var result = await useCase.ExecuteAsync(useCaseRequest, cancellationToken);

                if (result.IsSuccess)
                {
                    logger.LogCritical("ADMIN_AUDIT: Service deleted successfully - ServiceId: {ServiceId}, UserId: {UserId}, Reason: {Reason}", 
                        id, requestContext.UserId, request.DeletionReason);
                    
                    return Results.Ok(new DeleteServiceApiResponse
                    {
                        Success = true,
                        ServiceId = result.Value.ServiceId,
                        DeletedAt = result.Value.DeletedAt,
                        DeletionType = result.Value.DeletionType,
                        ServiceSnapshot = result.Value.ServiceSnapshot,
                        AuditTrail = result.Value.AuditTrail,
                        PerformanceMetrics = result.Value.PerformanceMetrics
                    });
                }

                if (result.Error.Code == "SERVICE_NOT_FOUND")
                {
                    return Results.NotFound(new AdminErrorResponse
                    {
                        Success = false,
                        Message = result.Error.Message,
                        Code = result.Error.Code,
                        RequestId = requestContext.RequestId
                    });
                }

                return Results.BadRequest(new AdminErrorResponse
                {
                    Success = false,
                    Message = result.Error.Message,
                    Code = result.Error.Code,
                    RequestId = requestContext.RequestId
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ADMIN_AUDIT: Service deletion failed with exception - ServiceId: {ServiceId}, UserId: {UserId}", 
                    id, requestContext.UserId);
                return Results.Problem("Internal server error occurred during service deletion");
            }
        });

        // PATCH /admin/api/services/{id}/publish - Publish/unpublish service
        adminApiGroup.MapPatch("/services/{id}/publish", async (
            string id,
            [FromBody] PublishServiceApiRequest request,
            IUpdateServiceUseCase useCase,
            ILogger<Program> logger,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var requestContext = ExtractMedicalGradeRequestContext(context, request.RequestId);
            AddObservabilityHeaders(context, requestContext.CorrelationId, requestContext.RequestId);

            try
            {
                // Use UpdateServiceUseCase to change availability status
                var useCaseRequest = new UpdateServiceRequest
                {
                    ServiceId = id,
                    Available = request.Publish,
                    UserContext = requestContext.UserId,
                    RequestId = requestContext.RequestId,
                    ClientIpAddress = requestContext.IpAddress,
                    UserAgent = requestContext.UserAgent
                };

                var result = await useCase.ExecuteAsync(useCaseRequest, cancellationToken);

                if (result.IsSuccess)
                {
                    var action = request.Publish ? "published" : "unpublished";
                    logger.LogInformation("ADMIN_AUDIT: Service {Action} successfully - ServiceId: {ServiceId}, UserId: {UserId}", 
                        action, id, requestContext.UserId);
                    
                    return Results.Ok(new PublishServiceApiResponse
                    {
                        Success = true,
                        ServiceId = result.Value.ServiceId,
                        Published = request.Publish,
                        AuditTrail = result.Value.AuditTrail,
                        PerformanceMetrics = result.Value.PerformanceMetrics
                    });
                }

                if (result.Error.Code == "SERVICE_NOT_FOUND")
                {
                    return Results.NotFound(new AdminErrorResponse
                    {
                        Success = false,
                        Message = result.Error.Message,
                        Code = result.Error.Code,
                        RequestId = requestContext.RequestId
                    });
                }

                return Results.BadRequest(new AdminErrorResponse
                {
                    Success = false,
                    Message = result.Error.Message,
                    Code = result.Error.Code,
                    RequestId = requestContext.RequestId
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ADMIN_AUDIT: Service publish/unpublish failed with exception - ServiceId: {ServiceId}, UserId: {UserId}", 
                    id, requestContext.UserId);
                return Results.Problem("Internal server error occurred during service publish/unpublish");
            }
        });

        return app;
    }

    private static MedicalGradeRequestContext ExtractMedicalGradeRequestContext(HttpContext context, string? providedRequestId = null)
    {
        // Extract correlation ID from headers
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
        
        // Extract request ID (prefer provided, fallback to header, then generate)
        var requestId = providedRequestId 
                       ?? context.Request.Headers["X-Request-ID"].FirstOrDefault() 
                       ?? Guid.NewGuid().ToString();

        // Extract user context - in production this would come from authentication
        var userId = context.User?.Identity?.Name ?? "anonymous";
        
        // Extract client IP address (handle load balancer scenarios)
        var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0] 
                       ?? context.Request.Headers["X-Real-IP"].FirstOrDefault() 
                       ?? context.Connection.RemoteIpAddress?.ToString() 
                       ?? "unknown";

        // Extract User Agent
        var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";

        return new MedicalGradeRequestContext
        {
            CorrelationId = correlationId,
            RequestId = requestId,
            UserId = userId,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
    }

    private static void AddObservabilityHeaders(HttpContext context, string correlationId, string requestId)
    {
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        context.Response.Headers["X-Request-ID"] = requestId;
    }
}

/// <summary>
/// Medical-grade request context for audit compliance
/// </summary>
public class MedicalGradeRequestContext
{
    public string CorrelationId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
}

// API Request/Response DTOs for Admin endpoints
public class CreateServiceApiRequest
{
    [Required] public string Title { get; set; } = string.Empty;
    [Required] public string Slug { get; set; } = string.Empty;
    [Required] public string Description { get; set; } = string.Empty;
    [Required] public string DetailedDescription { get; set; } = string.Empty;
    public string[]? Technologies { get; set; }
    public string[]? Features { get; set; }
    public string[]? DeliveryModes { get; set; }
    public string? Icon { get; set; }
    public string? Image { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public bool? Available { get; set; }
    public string? RequestId { get; set; }
}

public class UpdateServiceApiRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? DetailedDescription { get; set; }
    public string[]? Technologies { get; set; }
    public string[]? Features { get; set; }
    public string[]? DeliveryModes { get; set; }
    public string? Icon { get; set; }
    public string? Image { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public bool? Available { get; set; }
    public string? RequestId { get; set; }
}

public class DeleteServiceApiRequest
{
    [Required] public string DeletionReason { get; set; } = string.Empty;
    public bool ForceDelete { get; set; } = false;
    public string? RequestId { get; set; }
}

public class PublishServiceApiRequest
{
    [Required] public bool Publish { get; set; }
    public string? RequestId { get; set; }
}

// API Response DTOs
public class CreateServiceApiResponse
{
    public bool Success { get; set; }
    public string ServiceId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool Available { get; set; }
    public List<AuditEntryDto> AuditTrail { get; set; } = new();
}

public class UpdateServiceApiResponse
{
    public bool Success { get; set; }
    public string ServiceId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool Available { get; set; }
    public Dictionary<string, (object? OldValue, object? NewValue)> Changes { get; set; } = new();
    public List<AdminAuditLogEntry> AuditTrail { get; set; } = new();
    public AdminPerformanceMetrics PerformanceMetrics { get; set; } = null!;
}

public class AdminServiceDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool Available { get; set; }
}

public class AuditEntryDto
{
    public string Operation { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Changes { get; set; } = string.Empty;
}


public class DeleteServiceApiResponse
{
    public bool Success { get; set; }
    public string ServiceId { get; set; } = string.Empty;
    public DateTime DeletedAt { get; set; }
    public string DeletionType { get; set; } = string.Empty;
    public ServiceSnapshot ServiceSnapshot { get; set; } = null!;
    public List<AdminAuditLogEntry> AuditTrail { get; set; } = new();
    public AdminPerformanceMetrics PerformanceMetrics { get; set; } = null!;
}

public class PublishServiceApiResponse
{
    public bool Success { get; set; }
    public string ServiceId { get; set; } = string.Empty;
    public bool Published { get; set; }
    public List<AdminAuditLogEntry> AuditTrail { get; set; } = new();
    public AdminPerformanceMetrics PerformanceMetrics { get; set; } = null!;
}

public class AdminErrorResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
}