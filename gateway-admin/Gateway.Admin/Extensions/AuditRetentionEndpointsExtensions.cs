using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using InternationalCenter.Shared.Extensions;
using InternationalCenter.Shared.Services;

namespace InternationalCenter.Gateway.Admin.Extensions;

/// <summary>
/// Admin Gateway endpoints for audit retention management and compliance reporting
/// Provides secure access to retention reports and manual cleanup operations
/// Requires administrator authentication for medical-grade compliance access
/// </summary>
public static class AuditRetentionEndpointsExtensions
{
    /// <summary>
    /// Maps audit retention management endpoints to the Admin Gateway
    /// Provides authenticated access to retention reports and cleanup operations
    /// </summary>
    public static IEndpointRouteBuilder MapAuditRetentionEndpoints(this IEndpointRouteBuilder app)
    {
        var retentionGroup = app.MapGroup("/admin/audit/retention")
            .RequireAuthorization() // Require authenticated admin user
            .WithTags("Audit Retention")
            .WithOpenApi();

        // GET /admin/audit/retention/report - Generate compliance report
        retentionGroup.MapGet("/report", async (
            IServiceProvider serviceProvider,
            DateTime? reportDate = null) =>
        {
            try
            {
                var report = await serviceProvider.GenerateAuditRetentionReportAsync(reportDate);
                return Results.Ok(new
                {
                    success = true,
                    data = report,
                    message = "Audit retention compliance report generated successfully"
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Retention Report Generation Failed",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("GenerateAuditRetentionReport")
        .WithSummary("Generate medical-grade audit retention compliance report")
        .WithDescription("Generates a comprehensive compliance report showing audit log retention status according to medical-grade healthcare regulations")
        .Produces<object>(200)
        .Produces<ProblemDetails>(500);

        // GET /admin/audit/retention/status - Quick retention status
        retentionGroup.MapGet("/status", async (IServiceProvider serviceProvider) =>
        {
            try
            {
                var report = await serviceProvider.GenerateAuditRetentionReportAsync();
                return Results.Ok(new
                {
                    success = true,
                    data = new
                    {
                        complianceStatus = report.ComplianceStatus,
                        compliancePercentage = report.CompliancePercentage,
                        totalAuditLogs = report.TotalAuditLogs,
                        expiredAuditLogs = report.ExpiredAuditLogs,
                        retentionDays = report.RetentionDays,
                        nextCleanup = report.NextCleanupEstimate,
                        summary = report.Summary
                    },
                    message = "Audit retention status retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Retention Status Check Failed", 
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("GetAuditRetentionStatus")
        .WithSummary("Get current audit retention compliance status")
        .WithDescription("Returns quick compliance status and key metrics for audit log retention")
        .Produces<object>(200)
        .Produces<ProblemDetails>(500);

        // POST /admin/audit/retention/cleanup - Manual cleanup (use carefully)
        retentionGroup.MapPost("/cleanup", [Authorize(Roles = "Admin,SuperAdmin")] async (
            IServiceProvider serviceProvider,
            ILogger<Program> logger,
            int? customRetentionDays = null) =>
        {
            try
            {
                logger.LogWarning("MEDICAL_AUDIT_RETENTION: Manual cleanup initiated by admin user with custom retention days: {CustomRetentionDays}", customRetentionDays);
                
                var deletedCount = await serviceProvider.PerformImmediateAuditRetentionCleanupAsync(customRetentionDays);
                
                logger.LogInformation("MEDICAL_AUDIT_RETENTION: Manual cleanup completed by admin - DeletedCount: {DeletedCount}", deletedCount);
                
                return Results.Ok(new
                {
                    success = true,
                    data = new
                    {
                        deletedCount = deletedCount,
                        retentionDays = customRetentionDays,
                        cleanupTimestamp = DateTime.UtcNow
                    },
                    message = $"Manual audit retention cleanup completed. {deletedCount} expired records removed."
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "MEDICAL_AUDIT_RETENTION: Manual cleanup failed");
                
                return Results.Problem(
                    title: "Manual Cleanup Failed",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("PerformManualAuditCleanup")
        .WithSummary("Perform immediate audit retention cleanup (Admin only)")
        .WithDescription("Manually triggers audit log cleanup - requires Admin or SuperAdmin role. Use with caution in production.")
        .Produces<object>(200)
        .Produces<ProblemDetails>(500)
        .Produces<ProblemDetails>(403);

        return app;
    }
}