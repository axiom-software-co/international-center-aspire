using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Shared.Middleware;

namespace Shared.Extensions;

public static class ZeroTrustMiddlewareExtensions
{
    public static IApplicationBuilder UseZeroTrustSecurity(this IApplicationBuilder app)
    {
        // Add zero-trust security middleware early in the pipeline
        // but after basic ASP.NET Core middleware and before authentication
        app.UseMiddleware<ZeroTrustSecurityMiddleware>();
        
        return app;
    }

    public static IServiceCollection AddZeroTrustMiddleware(this IServiceCollection services)
    {
        // Register any required services for the zero-trust middleware
        // Currently no additional services needed, but keeping for future expansion
        
        return services;
    }
}