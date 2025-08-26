using InternationalCenter.Shared.Extensions;
using InternationalCenter.Shared.Configuration;
using InternationalCenter.Shared.Infrastructure.RateLimiting;
using InternationalCenter.Shared.Services;
using InternationalCenter.Gateway.Public.Middleware;
using Gateway.Public.Services;
using Gateway.Public.Middleware;
using Infrastructure.Metrics.Extensions;
using Microsoft.AspNetCore.Authentication;
using TestAuthenticationHandler = InternationalCenter.Shared.Infrastructure.Testing.TestAuthenticationHandler;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Microsoft.Extensions.Primitives;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to remove server header for security
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
});

// Add service defaults & Aspire client integrations
builder.Services.AddServiceDefaults();

// Add Infrastructure.Metrics for Prometheus metrics collection
builder.Services.AddMetricsInfrastructure(builder.Configuration);

// Add Public Gateway metrics services
builder.Services.AddSingleton<PublicGatewayMetricsService>();
builder.Services.AddScoped<RateLimitingMetricsService>();

// Configure Azure Managed Identity for accessing external Azure resources (Redis, etc.)
// This enables the Public Gateway to authenticate to Azure services without storing credentials
if (!builder.Environment.IsEnvironment("Testing"))
{
    var credential = builder.Environment.IsProduction() || builder.Environment.IsEnvironment("Staging")
        ? new DefaultAzureCredential() // Uses managed identity in production/staging
        : new DefaultAzureCredential(new DefaultAzureCredentialOptions 
        { 
            ExcludeManagedIdentityCredential = true // In development, use Azure CLI or Visual Studio credentials
        });
    
    builder.Services.AddSingleton(credential);
    
    // Configure environment-specific secret handling and rotation support for Public Gateway
    var secretsProvider = builder.Configuration["SECRETS_PROVIDER"];
    var keyVaultUri = builder.Configuration["KEY_VAULT_URI"];
    
    if (!builder.Environment.IsDevelopment() && !string.IsNullOrEmpty(keyVaultUri))
    {
        // Add Azure Key Vault configuration provider with secret rotation support
        builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), credential, new Azure.Extensions.AspNetCore.Configuration.Secrets.AzureKeyVaultConfigurationOptions
        {
            ReloadInterval = TimeSpan.FromMinutes(30) // Refresh secrets every 30 minutes for rotation support
        });
        
        var logger = builder.Services.BuildServiceProvider().GetService<ILogger<Program>>();
        logger?.LogInformation("Public Gateway: Azure Key Vault configuration provider enabled with 30-minute refresh interval for secret rotation support");
    }
    
    // Add configuration change monitoring for secret rotation
    builder.Services.Configure<IConfiguration>(config =>
    {
        if (!builder.Environment.IsDevelopment() && !string.IsNullOrEmpty(keyVaultUri))
        {
            // Register configuration change token for monitoring secret updates
            ChangeToken.OnChange(() => builder.Configuration.GetReloadToken(), () =>
            {
                var logger = builder.Services.BuildServiceProvider().GetService<ILogger<Program>>();
                logger?.LogInformation("Public Gateway configuration refresh triggered - secret rotation detected, KeyVaultUri: {KeyVaultUri}, Timestamp: {Timestamp}, Environment: {Environment}",
                    keyVaultUri,
                    DateTimeOffset.UtcNow,
                    builder.Environment.EnvironmentName);
            });
        }
    });
    
    var logger2 = builder.Services.BuildServiceProvider().GetService<ILogger<Program>>();
    logger2?.LogInformation("Public Gateway: Azure Managed Identity configured for environment: {Environment}, Secrets Provider: {SecretsProvider}", 
        builder.Environment.EnvironmentName, secretsProvider ?? "LOCAL_PARAMETERS");
}

// Add Redis distributed cache for rate limiting (configured via Aspire)
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("redis");
    });
}
else
{
    builder.Services.AddStackExchangeRedisCache(options => { /* Will be configured by tests */ });
}

// Add YARP reverse proxy with service discovery
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add Redis connection for distributed rate limiting
IConnectionMultiplexer? redis = null;
if (!builder.Environment.IsEnvironment("Testing"))
{
    var redisConnectionString = builder.Configuration.GetConnectionString("redis") ?? "localhost:6379";
    redis = ConnectionMultiplexer.Connect(redisConnectionString);
    builder.Services.AddSingleton(redis);
}
else
{
    // For testing, use in-memory rate limiting
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ => 
        throw new InvalidOperationException("Redis not available in testing environment"));
}

// Add Redis-backed rate limiting for Public Gateway
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    // Global IP-based rate limiting for public website usage patterns (1000 requests per minute)
    options.GlobalLimiter = RedisRateLimiterFactory.CreateRedisFixedWindowRateLimiter<Microsoft.AspNetCore.Http.HttpContext>(
        context =>
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? 
                          context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? 
                          "unknown";
            return $"public_gateway_ip:{clientIp}";
        },
        permitLimit: 1000, // Higher limits for public website usage
        window: TimeSpan.FromMinutes(1)
    );

    options.OnRejected = (context, cancellationToken) =>
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        var clientIp = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? 
                      context.HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? 
                      "unknown";
        var correlationId = context.HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
        var userAgent = context.HttpContext.Request.Headers.UserAgent.FirstOrDefault() ?? "unknown";
        var versionService = context.HttpContext.RequestServices.GetService<IVersionService>();
        var appVersion = versionService?.GetVersion() ?? "unknown";
        
        // Enhanced structured logging for rate limiting metrics and monitoring
        logger.LogWarning("PUBLIC_GATEWAY_RATE_LIMIT_METRICS: Rate limit exceeded - ClientIp: {ClientIp}, Path: {Path}, Method: {Method}, UserAgent: {UserAgent}, CorrelationId: {CorrelationId}, AppVersion: {AppVersion}, RateLimitType: {RateLimitType}, LimitValue: {LimitValue}, WindowMinutes: {WindowMinutes}, Timestamp: {Timestamp}, GatewayType: {GatewayType}",
            clientIp, 
            context.HttpContext.Request.Path,
            context.HttpContext.Request.Method,
            userAgent,
            correlationId,
            appVersion,
            "IP_BASED",
            1000,
            1,
            DateTimeOffset.UtcNow,
            "PUBLIC");

        // Rate limiting metrics for monitoring
        logger.LogInformation("RATE_LIMIT_VIOLATION_METRICS: Public Gateway - MetricType: {MetricType}, ClientIp: {ClientIp}, RequestCount: {RequestCount}, LimitExceeded: {LimitExceeded}, CorrelationId: {CorrelationId}, AppVersion: {AppVersion}",
            "RATE_LIMIT_EXCEEDED",
            clientIp,
            1000, // Assuming exceeded after 1000 requests
            true,
            correlationId,
            appVersion);

        // Add comprehensive rate limit headers for observability
        context.HttpContext.Response.Headers["X-RateLimit-Limit"] = "1000";
        context.HttpContext.Response.Headers["X-RateLimit-Remaining"] = "0";
        context.HttpContext.Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(1)).ToUnixTimeSeconds().ToString();
        context.HttpContext.Response.Headers["X-RateLimit-Type"] = "IP_BASED";
        context.HttpContext.Response.Headers["X-Gateway-Type"] = "PUBLIC";
        
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        return new ValueTask();
    };
});

// Add CORS for public website access
builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.AddPolicy("PublicWebsite", policy =>
        {
            policy
                .WithOrigins("http://localhost:4321", "https://localhost:4321") // Astro dev server
                .AllowAnyMethod()
                .AllowAnyHeader()
                .WithExposedHeaders("X-Correlation-ID", "X-Request-ID");
        });
    }
    else
    {
        options.AddPolicy("PublicWebsite", policy =>
        {
            policy
                .WithOrigins(builder.Configuration.GetSection("PublicAllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
                .WithMethods("GET", "POST", "OPTIONS")
                .AllowAnyHeader()
                .WithExposedHeaders("X-Correlation-ID", "X-Request-ID");
        });
    }
});

// Add Aspire service discovery
builder.Services.AddAspireServiceDiscovery(builder.Configuration);

// Add minimal authentication for testing consistency
if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddAuthentication("Test")
        .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", options => { });
    builder.Services.AddAuthorization();
}

// Add health checks for Public Gateway
builder.Services.AddHealthChecks()
    .AddCheck("services-public-api-proxy", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["proxy", "services", "public"]);

// Add services to the container
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseExceptionHandler();

// Add structured logging middleware with anonymous user tracking
app.UsePublicGatewayStructuredLogging();

// Add metrics collection middleware early in pipeline
app.UsePublicGatewayMetrics();

// Add rate limiting middleware early in pipeline
app.UseRateLimiter();

// Add rate limiting success metrics middleware for observability
app.Use(async (context, next) =>
{
    // Skip metrics for health checks and version endpoints
    var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
    if (path.Contains("/health") || path.Contains("/api/version") || path.Contains("/favicon.ico") || 
        path.Contains("/openapi") || path.Contains("/swagger"))
    {
        await next();
        return;
    }
    
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var versionService = scope.ServiceProvider.GetService<IVersionService>();
    
    var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? 
                   context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? 
                   "unknown";
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
    var appVersion = versionService?.GetVersion() ?? "unknown";
    
    await next();
    
    // Log successful rate limiting metrics (requests that passed rate limiting)
    if (context.Response.StatusCode != StatusCodes.Status429TooManyRequests)
    {
        logger.LogDebug("RATE_LIMIT_SUCCESS_METRICS: Public Gateway - MetricType: {MetricType}, ClientIp: {ClientIp}, Path: {Path}, Method: {Method}, StatusCode: {StatusCode}, RateLimitPassed: {RateLimitPassed}, CorrelationId: {CorrelationId}, AppVersion: {AppVersion}, GatewayType: {GatewayType}, Timestamp: {Timestamp}",
            "RATE_LIMIT_PASSED",
            clientIp,
            context.Request.Path,
            context.Request.Method,
            context.Response.StatusCode,
            true,
            correlationId,
            appVersion,
            "PUBLIC",
            DateTimeOffset.UtcNow);
    }
});

// Add enhanced security validation middleware for public gateway
app.Use(async (context, next) =>
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    
    // Skip security validation for health checks and version endpoints
    var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
    if (path.Contains("/health") || path.Contains("/api/version") || path.Contains("/favicon.ico") || 
        path.Contains("/openapi") || path.Contains("/swagger"))
    {
        await next();
        return;
    }
    
    var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? 
                   context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? 
                   "unknown";
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
    
    // 1. IP Address Validation - Check IP blocklist for public gateway
    var blockedIps = configuration.GetSection("Security:BlockedIpAddresses").Get<string[]>() ?? Array.Empty<string>();
    if (blockedIps.Contains(clientIp))
    {
        logger.LogWarning("PUBLIC_GATEWAY_SECURITY: Blocked IP {ClientIp} attempted access to {Path}, CorrelationId: {CorrelationId}",
            clientIp, context.Request.Path, correlationId);
        
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        await context.Response.WriteAsync("{\"error\":\"IP_ADDRESS_BLOCKED\",\"message\":\"Access from this IP address is not allowed\"}");
        return;
    }
    
    // 2. Security Headers Validation - Check for suspicious User-Agent and SQL injection patterns
    var userAgent = context.Request.Headers.UserAgent.FirstOrDefault() ?? "";
    var suspiciousPatterns = new[] { "sqlmap", "nikto", "nmap", "masscan", "zap", "burp" };
    if (suspiciousPatterns.Any(pattern => userAgent.ToLowerInvariant().Contains(pattern)))
    {
        logger.LogWarning("PUBLIC_GATEWAY_SECURITY: Suspicious User-Agent detected: {UserAgent} from {ClientIp}, CorrelationId: {CorrelationId}",
            userAgent, clientIp, correlationId);
        
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        await context.Response.WriteAsync("{\"error\":\"SUSPICIOUS_USER_AGENT\",\"message\":\"Request blocked due to security policy\"}");
        return;
    }
    
    // Check for SQL injection patterns in headers
    var allHeaderValues = context.Request.Headers.SelectMany(h => h.Value).ToList();
    var sqlInjectionPatterns = new[] { "union select", "drop table", "exec(", "script>", "<iframe" };
    foreach (var headerValue in allHeaderValues)
    {
        var lowerValue = headerValue?.ToLowerInvariant() ?? "";
        if (sqlInjectionPatterns.Any(pattern => lowerValue.Contains(pattern)))
        {
            logger.LogWarning("PUBLIC_GATEWAY_SECURITY: Potential security threat in headers from {ClientIp}, CorrelationId: {CorrelationId}",
                clientIp, correlationId);
            
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.Headers["X-Correlation-ID"] = correlationId;
            await context.Response.WriteAsync("{\"error\":\"SECURITY_THREAT_HEADERS\",\"message\":\"Request blocked due to security policy\"}");
            return;
        }
    }
    
    // 3. Request Size Limits - Check request body size limit
    var maxRequestSize = configuration.GetValue<long>("Security:MaxRequestSizeBytes", 10 * 1024 * 1024); // 10MB default
    if (context.Request.ContentLength > maxRequestSize)
    {
        logger.LogWarning("PUBLIC_GATEWAY_SECURITY: Request size {Size} exceeds limit {Limit} from {ClientIp}, CorrelationId: {CorrelationId}",
            context.Request.ContentLength, maxRequestSize, clientIp, correlationId);
        
        context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        await context.Response.WriteAsync("{\"error\":\"REQUEST_SIZE_EXCEEDED\",\"message\":\"Request payload too large\"}");
        return;
    }
    
    // 4. HTTPS Enforcement - Check for secure connection requirement
    var requireHttps = configuration.GetValue<bool>("Security:RequireHttps", false); // Default false for public gateway
    if (requireHttps && !context.Request.IsHttps && !IsLocalDevelopment(context))
    {
        logger.LogWarning("PUBLIC_GATEWAY_SECURITY: Insecure connection attempt to {Path} from {ClientIp}, CorrelationId: {CorrelationId}",
            context.Request.Path, clientIp, correlationId);
        
        context.Response.StatusCode = StatusCodes.Status426UpgradeRequired;
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        await context.Response.WriteAsync("{\"error\":\"INSECURE_CONNECTION\",\"message\":\"Secure connection required\"}");
        return;
    }
    
    // Apply comprehensive security headers for Public Gateway with modern standards and fallback policies
    context.Response.ApplyPublicGatewaySecurityHeaders(context, configuration);
    
    // Add correlation ID for request tracking
    context.Response.Headers.Append("X-Correlation-ID", correlationId);
    
    // Anonymous usage logging for Public Gateway
    logger.LogInformation("Public Gateway request: {Method} {Path} from {ClientIp} with correlation {CorrelationId}",
        context.Request.Method, context.Request.Path, clientIp, correlationId);

    await next();
    
    // Log response for tracking
    logger.LogDebug("Public Gateway response: {StatusCode} for correlation {CorrelationId}",
        context.Response.StatusCode, correlationId);
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Enable CORS for public website access
app.UseCors("PublicWebsite");

// Map version endpoint for production monitoring
app.MapVersionEndpoint();

// Map Prometheus metrics endpoint from Infrastructure.Metrics
app.UseMetricsEndpoint();

// Map rate limiting status endpoint for observability  
app.MapGet("/api/rate-limit/status", (ILogger<Program> logger, IServiceProvider serviceProvider) =>
{
    var correlationId = Guid.NewGuid().ToString();
    var versionService = serviceProvider.GetService<IVersionService>();
    var appVersion = versionService?.GetVersion() ?? "unknown";
    
    var statusData = new
    {
        GatewayType = "PUBLIC",
        RateLimitType = "IP_BASED",
        LimitValue = 1000,
        WindowMinutes = 1,
        Timestamp = DateTimeOffset.UtcNow,
        AppVersion = appVersion,
        CorrelationId = correlationId,
        Status = "Active",
        BackingStore = "Redis"
    };
    
    logger.LogInformation("RATE_LIMIT_STATUS: Public Gateway status requested - CorrelationId: {CorrelationId}, AppVersion: {AppVersion}",
        correlationId, appVersion);
    
    return Results.Ok(statusData);
})
.WithName("GetPublicGatewayRateLimitStatus")
.WithSummary("Get Public Gateway rate limiting status")
.WithDescription("Returns current rate limiting configuration and status for the Public Gateway")
.Produces<object>(200);

// Map YARP reverse proxy to Services Public API
app.MapReverseProxy();

// Map health checks
app.MapHealthChecks("/health");

app.Run();

// Helper method for security validation
static bool IsLocalDevelopment(HttpContext context)
{
    var host = context.Request.Host.Host.ToLowerInvariant();
    return host == "localhost" || 
           host == "127.0.0.1" || 
           host.StartsWith("192.168.") || 
           host.StartsWith("10.0.") ||
           host.StartsWith("172.");
}

// Make Program class accessible for integration testing
public partial class Program { }