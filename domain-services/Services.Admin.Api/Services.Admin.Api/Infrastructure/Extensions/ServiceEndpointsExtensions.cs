using InternationalCenter.Services.Admin.Api.Application.UseCases;
using InternationalCenter.Services.Admin.Api.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace InternationalCenter.Services.Admin.Api.Infrastructure.Extensions;

/// <summary>
/// TDD GREEN: Admin Service endpoints using ServiceHandlers following minimal API patterns
/// Focuses on HTTP protocol concerns with clean separation from business logic
/// </summary>
public static class ServiceEndpointsExtensions
{
    public static WebApplication MapAdminServiceEndpoints(this WebApplication app)
    {
        var serviceGroup = app.MapGroup("/api/admin/services")
            .WithTags("Admin Services")
            .WithOpenApi();

        // POST /api/admin/services - Create service 
        serviceGroup.MapPost("/", async (
            CreateServiceHandlerRequest request,
            ServiceHandlers handlers,
            ICreateServiceUseCase useCase,
            ILogger<ServiceHandlers> logger,
            HttpContext context) =>
        {
            return await ExecuteWithMedicalGradeAudit(context, () => 
                handlers.CreateService(request, useCase, logger));
        })
        .WithName("CreateService")
        .WithSummary("Create a new service");

        // PUT /api/admin/services/{id} - Update service
        serviceGroup.MapPut("/{id:guid}", async (
            Guid id,
            UpdateServiceHandlerRequest request,
            ServiceHandlers handlers,
            IUpdateServiceUseCase useCase,
            ILogger<ServiceHandlers> logger,
            HttpContext context) =>
        {
            return await ExecuteWithMedicalGradeAudit(context, () => 
                handlers.UpdateService(id.ToString(), request, useCase, logger));
        })
        .WithName("UpdateService")
        .WithSummary("Update an existing service");

        // DELETE /api/admin/services/{id} - Delete service
        serviceGroup.MapDelete("/{id:guid}", async (
            Guid id,
            ServiceHandlers handlers,
            IDeleteServiceUseCase useCase,
            ILogger<ServiceHandlers> logger,
            HttpContext context) =>
        {
            return await ExecuteWithMedicalGradeAudit(context, () => 
                handlers.DeleteService(id.ToString(), useCase, logger));
        })
        .WithName("DeleteService")
        .WithSummary("Delete a service");

        return app;
    }

    /// <summary>
    /// Execute admin operation with medical-grade audit trail and error handling
    /// </summary>
    private static async Task<IResult> ExecuteWithMedicalGradeAudit(
        HttpContext context, 
        Func<Task<IResult>> operation)
    {
        try
        {
            // Execute the operation
            var result = await operation();
            
            // Add medical-grade audit headers to successful responses
            AddMedicalGradeAuditHeaders(context);
            
            return result;
        }
        catch (Exception)
        {
            // Still add audit headers even for errors for traceability
            AddMedicalGradeAuditHeaders(context);
            
            // Re-throw to let ASP.NET Core handle error responses
            throw;
        }
    }

    /// <summary>
    /// Add medical-grade audit headers required for admin operations
    /// </summary>
    private static void AddMedicalGradeAuditHeaders(HttpContext context)
    {
        var requestId = context.TraceIdentifier;
        var auditTimestamp = DateTime.UtcNow.ToString("O"); // ISO 8601 format

        // Use indexer to set/overwrite headers instead of Add to avoid duplicates
        context.Response.Headers["X-Request-ID"] = requestId;
        context.Response.Headers["X-Audit-Timestamp"] = auditTimestamp;
        
        // Only set X-Correlation-ID if not already present to avoid conflicts
        if (!context.Response.Headers.ContainsKey("X-Correlation-ID"))
        {
            context.Response.Headers["X-Correlation-ID"] = requestId;
        }
    }
}