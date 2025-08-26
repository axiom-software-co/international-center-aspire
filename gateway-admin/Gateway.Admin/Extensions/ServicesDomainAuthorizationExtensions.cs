using Microsoft.AspNetCore.Authorization;
using InternationalCenter.Gateway.Admin.Authorization;
using InternationalCenter.Shared.Security;

namespace InternationalCenter.Gateway.Admin.Extensions;

/// <summary>
/// Extension methods for configuring Services domain authorization in Admin Gateway
/// Integrates with medical-grade audit logging and role-based access control
/// </summary>
public static class ServicesDomainAuthorizationExtensions
{
    /// <summary>
    /// Add Services domain authorization handlers for Admin Gateway
    /// Provides medical-grade compliance with comprehensive audit logging
    /// </summary>
    public static IServiceCollection AddServicesDomainAuthorization(this IServiceCollection services)
    {
        // Register Services domain authorization handlers
        services.AddScoped<IAuthorizationHandler, ServicesCreateAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, ServicesUpdateAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, ServicesDeleteAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, ServicesReadAuthorizationHandler>();

        return services;
    }

    /// <summary>
    /// Configure Services domain authorization policies for Admin Gateway
    /// Replaces simple role-based policies with comprehensive medical-grade authorization
    /// </summary>
    public static AuthorizationOptions ConfigureServicesDomainPolicies(this AuthorizationOptions options)
    {
        // Services.Create policy: requires ServiceAdmin role with medical-grade audit
        options.AddPolicy("Services.Create", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new ServicesCreateRequirement());
        });

        // Services.Update policy: requires ServiceAdmin or ServiceEditor roles
        options.AddPolicy("Services.Update", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new ServicesUpdateRequirement());
        });

        // Services.Delete policy: high-privilege operation with enhanced audit
        options.AddPolicy("Services.Delete", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new ServicesDeleteRequirement());
        });

        // Services.Read policy: basic authenticated access with role validation
        options.AddPolicy("Services.Read", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new ServicesReadRequirement());
        });

        return options;
    }

    /// <summary>
    /// Configure user context forwarding for Services Admin API
    /// Adds user and role information to outbound requests for domain operations
    /// </summary>
    public static IApplicationBuilder UseServicesDomainUserContextForwarding(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            // Add user context headers for Services Admin API
            if (context.Request.Path.StartsWithSegments("/api/admin/services"))
            {
                var user = context.User;
                if (user.Identity?.IsAuthenticated == true)
                {
                    // Extract user ID from Microsoft Entra External ID claims
                    var userId = user.FindFirst("sub")?.Value ?? 
                                user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? 
                                user.FindFirst("oid")?.Value ?? 
                                "system";

                    // Extract user roles
                    var userRoles = user.FindAll("roles")
                                      .Concat(user.FindAll(System.Security.Claims.ClaimTypes.Role))
                                      .Select(c => c.Value)
                                      .Distinct()
                                      .ToArray();

                    // Extract user name
                    var userName = user.FindFirst("name")?.Value ?? 
                                  user.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? 
                                  user.FindFirst("preferred_username")?.Value ?? 
                                  "unknown";

                    // Add headers for Services Admin API
                    context.Request.Headers["X-User-Id"] = userId;
                    context.Request.Headers["X-User-Name"] = userName;
                    context.Request.Headers["X-User-Roles"] = string.Join(",", userRoles);
                    
                    // Add correlation ID if not present
                    if (!context.Request.Headers.ContainsKey("X-Correlation-ID"))
                    {
                        context.Request.Headers["X-Correlation-ID"] = context.TraceIdentifier;
                    }

                    using var scope = app.ApplicationServices.CreateScope();
                    var logger = scope.ServiceProvider.GetService<ILogger<Program>>();
                    
                    logger?.LogDebug("Forwarding user context to Services Admin API: UserId={UserId}, Roles=[{UserRoles}]", 
                        userId, string.Join(",", userRoles));
                }
            }

            await next();
        });
    }

    /// <summary>
    /// Configure endpoint-specific authorization policies for YARP routes
    /// Maps HTTP methods to appropriate Services domain policies
    /// </summary>
    public static IEndpointRouteBuilder MapServicesDomainAuthorizationPolicies(this IEndpointRouteBuilder endpoints)
    {
        // This method can be used to apply authorization policies to specific YARP routes
        // The actual policy enforcement is handled by the YARP reverse proxy configuration
        
        return endpoints;
    }
}