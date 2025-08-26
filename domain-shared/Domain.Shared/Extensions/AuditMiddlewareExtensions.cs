using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Middleware;

namespace Shared.Extensions;

public static class AuditMiddlewareExtensions
{
    public static IApplicationBuilder UseMedicalGradeAudit(this IApplicationBuilder app)
    {
        // Ensure session middleware is registered (required for session ID in audit context)
        app.UseSession();
        
        // Add audit middleware after authentication but before authorization
        // This ensures we have user context available for auditing
        app.UseMiddleware<AuditMiddleware>();
        
        return app;
    }

    public static IServiceCollection AddSessionSupport(this IServiceCollection services)
    {
        // Add session support required for audit context
        services.AddSession(options =>
        {
            options.Cookie.Name = "InternationalCenter.Session";
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
        });
        
        return services;
    }

    public static async Task InitializeAuditSystemAsync(this IServiceProvider serviceProvider)
    {
        try
        {
            // Ensure audit tables are created
            var auditTablesCreated = await serviceProvider.EnsureAuditTablesCreatedAsync();
            
            if (auditTablesCreated)
            {
                var logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AuditMiddleware>>();
                logger.LogInformation("Medical-grade audit system initialized successfully");
                
                // Log system startup event
                using var scope = serviceProvider.CreateScope();
                var auditService = scope.ServiceProvider.GetService<Shared.Services.IAuditService>();
                if (auditService != null)
                {
                    await auditService.LogSystemEventAsync(
                        "SYSTEM_STARTUP",
                        $"Application started with audit system enabled at {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}",
                        Shared.Models.AuditSeverity.Info);
                }
            }
            else
            {
                var logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AuditMiddleware>>();
                logger.LogWarning("Failed to initialize audit tables - audit system may not function correctly");
            }
        }
        catch (Exception ex)
        {
            var logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AuditMiddleware>>();
            logger.LogError(ex, "Failed to initialize medical-grade audit system");
        }
    }
}