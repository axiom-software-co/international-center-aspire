using InternationalCenter.Gateway.Admin.Services;
using InternationalCenter.Shared.Services;
using InternationalCenter.Shared.Models;
using InternationalCenter.Shared.Extensions;

namespace InternationalCenter.Gateway.Admin.Extensions;

/// <summary>
/// Extension methods for configuring enhanced audit services in Admin Gateway
/// Provides medical-grade compliance audit logging with authentication context
/// </summary>
public static class AdminGatewayAuditExtensions
{
    /// <summary>
    /// Adds enhanced audit service with authentication context for Admin Gateway
    /// Replaces the standard audit service with Admin Gateway-specific implementation
    /// that properly integrates with Microsoft Entra External ID authentication
    /// </summary>
    public static IServiceCollection AddAdminGatewayAuditServices(this IServiceCollection services, IConfiguration configuration, bool enableRetention = true)
    {
        // Add medical-grade audit infrastructure with default configuration and optional retention
        if (enableRetention)
        {
            services.AddMedicalGradeAuditRetentionWithDefaults();
        }
        else
        {
            services.AddMedicalGradeAuditWithDefaults();
        }
        
        // Replace standard audit service with Admin Gateway-enhanced version
        services.AddScoped<IAuditService, AdminGatewayAuditService>();
        
        // Ensure HttpContextAccessor is available for user context extraction
        services.AddHttpContextAccessor();
        
        return services;
    }

    /// <summary>
    /// Configures medical-grade audit middleware with authentication context forwarding
    /// Ensures user context is properly captured and forwarded to backend services
    /// </summary>
    public static IApplicationBuilder UseAdminGatewayAuditMiddleware(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            using var scope = app.ApplicationServices.CreateScope();
            var auditService = scope.ServiceProvider.GetService<IAuditService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            
            var startTime = DateTime.UtcNow;
            var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? context.TraceIdentifier;
            
            // Extract user context for audit
            var user = context.User;
            var userId = user?.Identity?.IsAuthenticated == true
                ? user.FindFirst("sub")?.Value ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "authenticated"
                : "anonymous";
            
            var userRoles = user?.Identity?.IsAuthenticated == true
                ? user.FindAll("roles").Concat(user.FindAll(System.Security.Claims.ClaimTypes.Role)).Select(c => c.Value).ToArray()
                : new[] { "anonymous" };

            try
            {
                // Log request start with user context
                if (auditService != null)
                {
                    await auditService.LogBusinessEventAsync(
                        "GATEWAY_REQUEST_START",
                        "AdminGateway",
                        context.TraceIdentifier,
                        new { Method = context.Request.Method, Path = context.Request.Path.ToString(), UserId = userId, UserRoles = userRoles },
                        AuditSeverity.Info);
                }

                await next();

                var duration = DateTime.UtcNow - startTime;
                
                // Log successful request completion
                if (auditService != null)
                {
                    await auditService.LogBusinessEventAsync(
                        "GATEWAY_REQUEST_SUCCESS",
                        "AdminGateway",
                        context.TraceIdentifier,
                        new { Method = context.Request.Method, Path = context.Request.Path.ToString(), StatusCode = context.Response.StatusCode, DurationMs = duration.TotalMilliseconds, UserId = userId },
                        AuditSeverity.Info);
                }

                logger.LogInformation("ADMIN_GATEWAY_AUDIT: Request completed successfully - Method: {Method}, Path: {Path}, UserId: {UserId}, UserRoles: [{UserRoles}], StatusCode: {StatusCode}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                    context.Request.Method,
                    context.Request.Path,
                    userId,
                    string.Join(",", userRoles),
                    context.Response.StatusCode,
                    duration.TotalMilliseconds,
                    correlationId);
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                
                // Log failed request with user context
                if (auditService != null)
                {
                    await auditService.LogSecurityEventAsync(
                        "GATEWAY_REQUEST_ERROR",
                        $"Admin Gateway error processing {context.Request.Method} {context.Request.Path} for user {userId}: {ex.Message}",
                        AuditSeverity.Error);
                }

                logger.LogError(ex, "ADMIN_GATEWAY_AUDIT: Request failed - Method: {Method}, Path: {Path}, UserId: {UserId}, UserRoles: [{UserRoles}], Duration: {Duration}ms, CorrelationId: {CorrelationId}, Error: {Error}",
                    context.Request.Method,
                    context.Request.Path,
                    userId,
                    string.Join(",", userRoles),
                    duration.TotalMilliseconds,
                    correlationId,
                    ex.Message);

                throw; // Re-throw to maintain error handling pipeline
            }
        });
    }

    /// <summary>
    /// Initializes the Admin Gateway audit system with enhanced authentication context
    /// Logs system startup and validates audit service configuration
    /// </summary>
    public static async Task<IServiceProvider> InitializeAdminGatewayAuditSystemAsync(this IServiceProvider serviceProvider)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var auditService = scope.ServiceProvider.GetService<IAuditService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            
            if (auditService != null)
            {
                await auditService.LogSystemEventAsync(
                    "ADMIN_GATEWAY_STARTUP",
                    "Admin Gateway with enhanced authentication context audit system started successfully",
                    AuditSeverity.Info);
                
                logger.LogInformation("ADMIN_GATEWAY_AUDIT: Enhanced audit system with authentication context initialized successfully");
            }
            else
            {
                logger.LogWarning("ADMIN_GATEWAY_AUDIT: Audit service not found during initialization");
            }
        }
        catch (Exception ex)
        {
            var logger = serviceProvider.GetService<ILogger<Program>>();
            logger?.LogError(ex, "ADMIN_GATEWAY_AUDIT: Failed to initialize enhanced audit system");
        }
        
        return serviceProvider;
    }
}