using InternationalCenter.Services.Public.Api.Application.UseCases;
using InternationalCenter.Services.Domain.Models;
using InternationalCenter.Services.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.ComponentModel.DataAnnotations;

namespace InternationalCenter.Services.Public.Api.Extensions;

/// <summary>
/// REST API endpoints for Services Public API using minimal APIs
/// Integrates with existing ServiceQueryUseCase following Microsoft patterns
/// </summary>
public static class RestApiExtensions
{
    public static WebApplication MapServicesRestApi(this WebApplication app)
    {
        var apiGroup = app.MapGroup("/api")
            .WithTags("Services")
            .WithOpenApi();

        // GET /api/services - Get paginated services with filtering
        apiGroup.MapGet("/services", async (
            [FromServices] IServiceQueryUseCase useCase,
            [FromServices] ILogger<Program> logger,
            HttpContext context,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? category = null,
            [FromQuery] bool? featured = null,
            [FromQuery] bool availableOnly = true,
            [FromQuery] string sortBy = "priority") =>
        {
            using var activity = Activity.Current?.Source.StartActivity("GET:/api/services");
            
            var request = new ServicesQueryRequest
            {
                Page = Math.Max(1, page),
                PageSize = Math.Min(Math.Max(1, pageSize), 100), // Cap at 100
                Category = category,
                Featured = featured,
                AvailableOnly = availableOnly,
                SortBy = sortBy,
                
                // Extract request context for audit
                UserContext = GetUserContext(context),
                RequestId = GetRequestId(context),
                ClientIpAddress = GetClientIpAddress(context),
                UserAgent = GetUserAgent(context)
            };

            // Validate pagination parameters
            if (request.Page <= 0 || request.PageSize <= 0)
            {
                return Results.BadRequest(new { 
                    Message = "Page and PageSize must be greater than 0",
                    Code = "INVALID_PAGINATION" 
                });
            }

            var result = await useCase.ExecuteAsync(request);
            
            if (result.IsFailure)
            {
                logger.LogError("Service query failed: {Error}", result.Error?.Message);
                return Results.Problem(
                    detail: result.Error?.Message,
                    statusCode: 500,
                    title: "Service Query Failed");
            }

            // Add observability headers
            context.Response.Headers["X-Correlation-ID"] = request.RequestId;
            context.Response.Headers["X-Request-Id"] = request.RequestId;
            
            var response = new ServicesApiResponse
            {
                Services = result.Value.Services.Select(MapFromDomainService).ToList(),
                Pagination = new PaginationResponse
                {
                    Page = result.Value.Pagination.Page,
                    PageSize = result.Value.Pagination.PageSize,
                    Total = (int)result.Value.Pagination.Total,
                    TotalPages = result.Value.Pagination.TotalPages
                }
            };

            return Results.Ok(response);
        })
        .WithName("GetServices")
        .WithSummary("Get paginated services with optional filtering")
        .Produces<ServicesApiResponse>();

        // GET /api/services/search - Search services
        apiGroup.MapGet("/services/search", async (
            [FromServices] IServiceQueryUseCase useCase,
            [FromServices] ILogger<Program> logger,
            HttpContext context,
            [FromQuery, Required] string q,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? category = null,
            [FromQuery] string sortBy = "priority") =>
        {
            using var activity = Activity.Current?.Source.StartActivity("GET:/api/services/search");
            
            if (string.IsNullOrWhiteSpace(q))
            {
                return Results.BadRequest(new { 
                    Message = "Search query 'q' is required",
                    Code = "MISSING_QUERY" 
                });
            }

            var request = new ServicesQueryRequest
            {
                Page = Math.Max(1, page),
                PageSize = Math.Min(Math.Max(1, pageSize), 100),
                Category = category,
                SearchTerm = q.Trim(),
                SortBy = sortBy,
                
                // Extract request context for audit
                UserContext = GetUserContext(context),
                RequestId = GetRequestId(context),
                ClientIpAddress = GetClientIpAddress(context),
                UserAgent = GetUserAgent(context)
            };

            var result = await useCase.ExecuteAsync(request);
            
            if (result.IsFailure)
            {
                logger.LogError("Service search failed: {Error}", result.Error?.Message);
                return Results.Problem(
                    detail: result.Error?.Message,
                    statusCode: 500,
                    title: "Service Search Failed");
            }

            // Add observability headers
            context.Response.Headers["X-Correlation-ID"] = request.RequestId;
            context.Response.Headers["X-Request-Id"] = request.RequestId;
            
            var response = new SearchApiResponse
            {
                Services = result.Value.Services.Select(MapFromDomainService).ToList(),
                Pagination = new PaginationResponse
                {
                    Page = result.Value.Pagination.Page,
                    PageSize = result.Value.Pagination.PageSize,
                    Total = (int)result.Value.Pagination.Total,
                    TotalPages = result.Value.Pagination.TotalPages
                },
                Query = q
            };

            return Results.Ok(response);
        })
        .WithName("SearchServices")
        .WithSummary("Search services by query term")
        .Produces<SearchApiResponse>();

        // GET /api/services/featured - Get featured services
        apiGroup.MapGet("/services/featured", async (
            [FromServices] IServiceQueryUseCase useCase,
            [FromServices] ILogger<Program> logger,
            HttpContext context,
            [FromQuery] int limit = 10) =>
        {
            using var activity = Activity.Current?.Source.StartActivity("GET:/api/services/featured");
            
            var request = new ServicesQueryRequest
            {
                Page = 1,
                PageSize = Math.Min(Math.Max(1, limit), 50), // Cap featured services at 50
                Featured = true,
                AvailableOnly = true,
                SortBy = "priority",
                
                // Extract request context for audit
                UserContext = GetUserContext(context),
                RequestId = GetRequestId(context),
                ClientIpAddress = GetClientIpAddress(context),
                UserAgent = GetUserAgent(context)
            };

            var result = await useCase.ExecuteAsync(request);
            
            if (result.IsFailure)
            {
                logger.LogError("Featured services query failed: {Error}", result.Error?.Message);
                return Results.Problem(
                    detail: result.Error?.Message,
                    statusCode: 500,
                    title: "Featured Services Query Failed");
            }

            // Add observability headers
            context.Response.Headers["X-Correlation-ID"] = request.RequestId;
            context.Response.Headers["X-Request-Id"] = request.RequestId;
            
            var response = new FeaturedServicesApiResponse
            {
                Services = result.Value.Services.Select(MapFromDomainService).ToList()
            };

            return Results.Ok(response);
        })
        .WithName("GetFeaturedServices")
        .WithSummary("Get featured services")
        .Produces<FeaturedServicesApiResponse>();

        // GET /api/services/{slug} - Get service by slug
        apiGroup.MapGet("/services/{slug}", async (
            [FromServices] IServiceQueryUseCase useCase,
            [FromServices] ILogger<Program> logger,
            HttpContext context,
            string slug) =>
        {
            using var activity = Activity.Current?.Source.StartActivity("GET:/api/services/{slug}");
            
            if (string.IsNullOrWhiteSpace(slug))
            {
                return Results.BadRequest(new { 
                    Message = "Service slug is required",
                    Code = "MISSING_SLUG" 
                });
            }

            // Use search functionality to find by slug
            var request = new ServicesQueryRequest
            {
                Page = 1,
                PageSize = 1,
                SearchTerm = slug, // ServiceQueryUseCase should handle slug-based lookup
                AvailableOnly = true,
                
                // Extract request context for audit
                UserContext = GetUserContext(context),
                RequestId = GetRequestId(context),
                ClientIpAddress = GetClientIpAddress(context),
                UserAgent = GetUserAgent(context)
            };

            var result = await useCase.ExecuteAsync(request);
            
            if (result.IsFailure)
            {
                logger.LogError("Service by slug query failed: {Error}", result.Error?.Message);
                return Results.Problem(
                    detail: result.Error?.Message,
                    statusCode: 500,
                    title: "Service Lookup Failed");
            }

            var service = result.Value.Services.FirstOrDefault(s => s.Slug == slug);
            
            if (service == null)
            {
                return Results.NotFound(new { 
                    Message = $"Service with slug '{slug}' not found",
                    Code = "SERVICE_NOT_FOUND" 
                });
            }

            // Add observability headers
            context.Response.Headers["X-Correlation-ID"] = request.RequestId;
            context.Response.Headers["X-Request-Id"] = request.RequestId;
            
            var response = new ServiceApiResponse
            {
                Service = MapFromDomainService(service)
            };

            return Results.Ok(response);
        })
        .WithName("GetServiceBySlug")
        .WithSummary("Get service by slug")
        .Produces<ServiceApiResponse>()
        .Produces(404);

        return app;
    }

    // Helper methods for request context extraction (standard observability)
    private static string GetUserContext(HttpContext context)
    {
        return context.Request.Headers["X-User-Id"].FirstOrDefault() ?? 
               context.User.Identity?.Name ?? 
               "anonymous";
    }

    private static string GetRequestId(HttpContext context)
    {
        return context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ??
               context.Request.Headers["X-Request-Id"].FirstOrDefault() ??
               context.TraceIdentifier;
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').First().Trim();
        }
        
        return context.Request.Headers["X-Real-IP"].FirstOrDefault() ??
               context.Connection.RemoteIpAddress?.ToString() ??
               "unknown";
    }

    private static string GetUserAgent(HttpContext context)
    {
        return context.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";
    }

    // Map from domain entity to REST API DTO
    private static ServiceDto MapFromDomainService(Service domainService)
    {
        return new ServiceDto
        {
            Id = domainService.Id,
            Title = domainService.Title,
            Slug = domainService.Slug,
            Description = domainService.Description,
            DetailedDescription = domainService.DetailedDescription,
            Technologies = domainService.Metadata.Technologies.ToList(),
            Features = domainService.Metadata.Features.ToList(),
            DeliveryModes = domainService.Metadata.DeliveryModes.ToList(),
            Icon = domainService.Metadata.Icon,
            Image = domainService.Metadata.Image,
            Status = domainService.Status.ToString().ToLowerInvariant(),
            Available = domainService.Available,
            Featured = domainService.Featured,
            Category = domainService.Category?.Name ?? "",
            MetaTitle = domainService.Metadata.MetaTitle,
            MetaDescription = domainService.Metadata.MetaDescription,
            CreatedAt = domainService.CreatedAt,
            UpdatedAt = domainService.UpdatedAt
        };
    }
}

// REST API Response DTOs
public class ServicesApiResponse
{
    public List<ServiceDto> Services { get; set; } = new();
    public PaginationResponse Pagination { get; set; } = new();
}

public class SearchApiResponse
{
    public List<ServiceDto> Services { get; set; } = new();
    public PaginationResponse Pagination { get; set; } = new();
    public string Query { get; set; } = string.Empty;
}

public class FeaturedServicesApiResponse
{
    public List<ServiceDto> Services { get; set; } = new();
}

public class ServiceApiResponse
{
    public ServiceDto Service { get; set; } = new();
}

public class ServiceDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DetailedDescription { get; set; } = string.Empty;
    public List<string> Technologies { get; set; } = new();
    public List<string> Features { get; set; } = new();
    public List<string> DeliveryModes { get; set; } = new();
    public string Icon { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool Available { get; set; }
    public bool Featured { get; set; }
    public string Category { get; set; } = string.Empty;
    public string MetaTitle { get; set; } = string.Empty;
    public string MetaDescription { get; set; } = string.Empty;
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