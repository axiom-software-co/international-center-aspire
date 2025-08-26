using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Shared.Services;

namespace Shared.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Maps the production version endpoint as required by medical-grade compliance
    /// Returns version in format: Date.BuildNumber.ShortGitSha
    /// </summary>
    public static WebApplication MapVersionEndpoint(this WebApplication app)
    {
        app.MapGet("/api/version", (IVersionService versionService) =>
        {
            return Results.Ok(new
            {
                version = versionService.GetVersion(),
                buildDate = versionService.BuildDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                buildNumber = versionService.BuildNumber,
                shortGitSha = versionService.ShortGitSha,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            });
        })
        .WithName("GetVersion")
        .Produces<object>(200)
        .WithSummary("Get application version information")
        .WithDescription("Returns version in format Date.BuildNumber.ShortGitSha for production monitoring and medical-grade compliance");

        return app;
    }
}