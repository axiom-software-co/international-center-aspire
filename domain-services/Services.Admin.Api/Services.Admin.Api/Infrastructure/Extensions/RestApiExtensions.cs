using InternationalCenter.Services.Admin.Api.Application.UseCases;
using InternationalCenter.Services.Public.Api.Application.UseCases;
using InternationalCenter.Services.Domain.Models;
using InternationalCenter.Services.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using System.Diagnostics;

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
            .WithOpenApi(); 
            // Authentication handled by Admin Gateway - API focuses on domain business logic
            
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

        // GET /admin/api/services - Get all services with admin details
        adminApiGroup.MapGet("/services", async (
            [FromServices] IServiceQueryUseCase serviceQueryUseCase,
            [FromServices] ILogger<Program> logger,
            HttpContext context,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? category = null,
            [FromQuery] bool? featured = null,
            [FromQuery] bool includeInactive = true, // Admin can see inactive services
            [FromQuery] string sortBy = "updated-desc",
            CancellationToken cancellationToken = default) =>
        {
            var requestContext = ExtractMedicalGradeRequestContext(context);
            AddObservabilityHeaders(context, requestContext.CorrelationId, requestContext.RequestId);

            try
            {
                // Create admin query request with broader access than public API
                var request = new ServicesQueryRequest
                {
                    Page = Math.Max(1, page),
                    PageSize = Math.Min(Math.Max(1, pageSize), 100),
                    Category = category,
                    Featured = featured,
                    AvailableOnly = !includeInactive, // Admin can see all services
                    SortBy = sortBy,
                    UserContext = requestContext.UserId,
                    RequestId = requestContext.RequestId,
                    ClientIpAddress = requestContext.IpAddress,
                    UserAgent = requestContext.UserAgent
                };

                var result = await serviceQueryUseCase.ExecuteAsync(request);
                
                if (result.IsFailure)
                {
                    logger.LogError("ADMIN_AUDIT: Services query failed - UserId: {UserId}, Error: {Error}", 
                        requestContext.UserId, result.Error?.Message);
                    return Results.Problem(
                        detail: result.Error?.Message,
                        statusCode: 500,
                        title: "Admin Services Query Failed");
                }

                logger.LogInformation("ADMIN_AUDIT: Services query executed successfully - UserId: {UserId}, Count: {Count}", 
                    requestContext.UserId, result.Value.Services.Count());
                
                var response = new AdminServicesApiResponse
                {
                    Services = result.Value.Services.Select(service => new AdminServiceDto
                    {
                        Id = service.Id,
                        Title = service.Title,
                        Slug = service.Slug,
                        Description = service.Description,
                        Status = service.Status.ToString(),
                        Available = service.Available,
                        Featured = service.Featured,
                        Category = service.Category?.Name ?? "",
                        Technologies = service.Metadata.Technologies.ToList(),
                        Features = service.Metadata.Features.ToList(),
                        DeliveryModes = service.Metadata.DeliveryModes.ToList(),
                        CreatedAt = service.CreatedAt,
                        UpdatedAt = service.UpdatedAt
                    }).ToList(),
                    Pagination = new PaginationResponse
                    {
                        Page = result.Value.Pagination.Page,
                        PageSize = result.Value.Pagination.PageSize,
                        Total = (int)result.Value.Pagination.Total,
                        TotalPages = result.Value.Pagination.TotalPages
                    }
                };

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ADMIN_AUDIT: Services query failed with exception - UserId: {UserId}", requestContext.UserId);
                return Results.Problem("Internal server error occurred during services query");
            }
        })
        .WithName("GetAdminServices")
        .WithSummary("Get all services with admin details")
        .Produces<AdminServicesApiResponse>();

        // GET /admin/api/services/{id} - Get service by ID with admin details
        adminApiGroup.MapGet("/services/{id}", async (
            string id,
            [FromServices] IServiceQueryUseCase serviceQueryUseCase,
            [FromServices] ILogger<Program> logger,
            HttpContext context,
            CancellationToken cancellationToken = default) =>
        {
            var requestContext = ExtractMedicalGradeRequestContext(context);
            AddObservabilityHeaders(context, requestContext.CorrelationId, requestContext.RequestId);

            try
            {
                var request = new ServicesQueryRequest
                {
                    Page = 1,
                    PageSize = 1,
                    SearchTerm = id, // Use ID-based lookup through search
                    AvailableOnly = false, // Admin can see inactive services
                    UserContext = requestContext.UserId,
                    RequestId = requestContext.RequestId,
                    ClientIpAddress = requestContext.IpAddress,
                    UserAgent = requestContext.UserAgent
                };

                var result = await serviceQueryUseCase.ExecuteAsync(request);
                
                if (result.IsFailure)
                {
                    logger.LogError("ADMIN_AUDIT: Service by ID query failed - ServiceId: {ServiceId}, UserId: {UserId}, Error: {Error}", 
                        id, requestContext.UserId, result.Error?.Message);
                    return Results.Problem(
                        detail: result.Error?.Message,
                        statusCode: 500,
                        title: "Admin Service Query Failed");
                }

                var service = result.Value.Services.FirstOrDefault(s => s.Id == id);
                
                if (service == null)
                {
                    logger.LogWarning("ADMIN_AUDIT: Service not found - ServiceId: {ServiceId}, UserId: {UserId}", 
                        id, requestContext.UserId);
                    return Results.NotFound(new AdminErrorResponse
                    {
                        Success = false,
                        Message = $"Service with ID '{id}' not found",
                        Code = "SERVICE_NOT_FOUND",
                        RequestId = requestContext.RequestId
                    });
                }

                logger.LogInformation("ADMIN_AUDIT: Service retrieved successfully - ServiceId: {ServiceId}, UserId: {UserId}", 
                    id, requestContext.UserId);
                
                var response = new AdminServiceApiResponse
                {
                    Service = new AdminServiceDetailDto
                    {
                        Id = service.Id,
                        Title = service.Title,
                        Slug = service.Slug,
                        Description = service.Description,
                        DetailedDescription = service.DetailedDescription,
                        Status = service.Status.ToString(),
                        Available = service.Available,
                        Featured = service.Featured,
                        Category = service.Category?.Name ?? "",
                        Technologies = service.Metadata.Technologies.ToList(),
                        Features = service.Metadata.Features.ToList(),
                        DeliveryModes = service.Metadata.DeliveryModes.ToList(),
                        Icon = service.Metadata.Icon,
                        Image = service.Metadata.Image,
                        MetaTitle = service.Metadata.MetaTitle,
                        MetaDescription = service.Metadata.MetaDescription,
                        CreatedAt = service.CreatedAt,
                        UpdatedAt = service.UpdatedAt
                    }
                };

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ADMIN_AUDIT: Service by ID query failed with exception - ServiceId: {ServiceId}, UserId: {UserId}", 
                    id, requestContext.UserId);
                return Results.Problem("Internal server error occurred during service query");
            }
        })
        .WithName("GetAdminServiceById")
        .WithSummary("Get service by ID with admin details")
        .Produces<AdminServiceApiResponse>()
        .Produces(404);

        // GET /admin/api/services/categories - Get all service categories for admin
        adminApiGroup.MapGet("/services/categories", async (
            [FromServices] GetServiceCategoriesUseCase useCase,
            [FromServices] ILogger<Program> logger,
            HttpContext context,
            [FromQuery] bool activeOnly = false, // Admin can see all categories by default
            CancellationToken cancellationToken = default) =>
        {
            var requestContext = ExtractMedicalGradeRequestContext(context);
            AddObservabilityHeaders(context, requestContext.CorrelationId, requestContext.RequestId);

            try
            {
                var result = await useCase.ExecuteAsync(activeOnly, cancellationToken);
                
                if (result.IsFailure)
                {
                    logger.LogError("ADMIN_AUDIT: Service categories query failed - UserId: {UserId}, Error: {Error}", 
                        requestContext.UserId, result.Error?.Message);
                    return Results.Problem(
                        detail: result.Error?.Message,
                        statusCode: 500,
                        title: "Admin Service Categories Query Failed");
                }

                logger.LogInformation("ADMIN_AUDIT: Service categories retrieved successfully - UserId: {UserId}, Count: {Count}", 
                    requestContext.UserId, result.Value.Count());
                
                var response = new AdminServiceCategoriesApiResponse
                {
                    Categories = result.Value.Select(category => new AdminServiceCategoryDto
                    {
                        Id = category.Id.ToString(),
                        Name = category.Name,
                        Description = category.Description ?? "",
                        Slug = category.Slug?.Value ?? "",
                        Icon = category.Icon ?? "",
                        SortOrder = category.SortOrder,
                        CreatedAt = category.CreatedAt,
                        UpdatedAt = category.UpdatedAt
                    }).ToList()
                };

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ADMIN_AUDIT: Service categories query failed with exception - UserId: {UserId}", requestContext.UserId);
                return Results.Problem("Internal server error occurred during service categories query");
            }
        })
        .WithName("GetAdminServiceCategories")
        .WithSummary("Get all service categories for admin")
        .Produces<AdminServiceCategoriesApiResponse>();

        // GET /admin/api/metrics - Prometheus metrics endpoint for Services Admin API
        adminApiGroup.MapGet("/metrics", async (
            [FromServices] ServicesAdminApiMetricsService metricsService,
            [FromServices] ILogger<Program> logger,
            HttpContext context,
            CancellationToken cancellationToken = default) =>
        {
            using var activity = Activity.Current?.Source.StartActivity("GET:/admin/api/metrics");
            
            var requestContext = ExtractMedicalGradeRequestContext(context);
            AddObservabilityHeaders(context, requestContext.CorrelationId, requestContext.RequestId);
            
            try
            {
                var metricsData = await metricsService.ExportMetricsAsync(cancellationToken);
                
                logger.LogDebug("ADMIN_AUDIT: Metrics endpoint accessed - UserId: {UserId}, RequestId: {RequestId}", 
                    requestContext.UserId, requestContext.RequestId);
                
                // Set Prometheus content type
                context.Response.ContentType = "text/plain; version=0.0.4; charset=utf-8";
                
                return Results.Content(metricsData, "text/plain; version=0.0.4; charset=utf-8");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ADMIN_AUDIT: Metrics export failed - UserId: {UserId}", requestContext.UserId);
                return Results.Problem("Failed to export metrics data");
            }
        })
        .WithName("GetAdminMetrics")
        .WithSummary("Get Prometheus metrics for Services Admin API monitoring")
        .Produces(200, contentType: "text/plain")
        .Produces(500);

        // GET /admin/api/version - Production endpoint versioning for Admin API
        adminApiGroup.MapGet("/version", (
            [FromServices] ILogger<Program> logger,
            HttpContext context) =>
        {
            using var activity = Activity.Current?.Source.StartActivity("GET:/admin/api/version");
            
            var requestContext = ExtractMedicalGradeRequestContext(context);
            AddObservabilityHeaders(context, requestContext.CorrelationId, requestContext.RequestId);
            
            // Version information for production deployment tracking
            // Format: <Date>.<BuildNumber>.<ShortGitSha>
            var versionInfo = new AdminVersionApiResponse
            {
                ApiName = "Services Admin API",
                Version = GetVersionInfo(),
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
                Timestamp = DateTime.UtcNow,
                Status = "Healthy",
                MedicalGradeCompliance = true,
                AuditLoggingEnabled = true
            };
            
            logger.LogInformation("ADMIN_AUDIT: Version endpoint accessed - Version: {Version}, UserId: {UserId}, RequestId: {RequestId}", 
                versionInfo.Version, requestContext.UserId, requestContext.RequestId);

            return Results.Ok(versionInfo);
        })
        .WithName("GetAdminVersion")
        .WithSummary("Get Admin API version information for production deployment tracking")
        .Produces<AdminVersionApiResponse>();

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

        // Extract user context from Admin Gateway headers
        var userId = context.Request.Headers["X-User-Id"].FirstOrDefault() ?? "system";
        
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

    // Get version information for production deployment tracking
    private static string GetVersionInfo()
    {
        try
        {
            // Check for CI-generated version file first (production pattern)
            var versionFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "version.txt");
            if (File.Exists(versionFile))
            {
                var version = File.ReadAllText(versionFile).Trim();
                if (!string.IsNullOrEmpty(version))
                {
                    return version;
                }
            }

            // Fallback: Generate development version with assembly info
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var buildDate = DateTime.UtcNow.ToString("yyyyMMdd");
            var buildNumber = "admin-dev";
            var gitSha = "local";

            return $"{buildDate}.{buildNumber}.{gitSha}";
        }
        catch
        {
            // Ultimate fallback for any errors
            return "unknown.admin-dev.local";
        }
    }
}

/// <summary>
/// Medical-grade request context for audit compliance
/// User context provided by Admin Gateway via headers
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
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool Available { get; set; }
    public bool Featured { get; set; }
    public string Category { get; set; } = string.Empty;
    public List<string> Technologies { get; set; } = new();
    public List<string> Features { get; set; } = new();
    public List<string> DeliveryModes { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
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

// Additional DTOs for GET endpoints
public class AdminServicesApiResponse
{
    public List<AdminServiceDto> Services { get; set; } = new();
    public PaginationResponse Pagination { get; set; } = new();
}

public class AdminServiceApiResponse
{
    public AdminServiceDetailDto Service { get; set; } = new();
}

public class AdminServiceDetailDto : AdminServiceDto
{
    public string DetailedDescription { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string MetaTitle { get; set; } = string.Empty;
    public string MetaDescription { get; set; } = string.Empty;
}

public class AdminServiceCategoriesApiResponse
{
    public List<AdminServiceCategoryDto> Categories { get; set; } = new();
}

public class AdminServiceCategoryDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PaginationResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
}

public class AdminVersionApiResponse
{
    public string ApiName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool MedicalGradeCompliance { get; set; }
    public bool AuditLoggingEnabled { get; set; }
}