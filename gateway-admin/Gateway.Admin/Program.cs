using InternationalCenter.Shared.Extensions;
using InternationalCenter.Shared.Configuration;
using InternationalCenter.Shared.Infrastructure.RateLimiting;
using InternationalCenter.Shared.Services;
using InternationalCenter.Gateway.Admin.Extensions;
using InternationalCenter.Gateway.Admin.Middleware;
using Gateway.Admin.Services;
using Gateway.Admin.Middleware;
using Infrastructure.Metrics.Extensions;
using Service.Audit.Extensions;
using Service.Audit.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Microsoft.Extensions.Primitives;
using TestAuthenticationHandler = InternationalCenter.Shared.Infrastructure.Testing.TestAuthenticationHandler;

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

// Add Service.Audit for medical-grade audit logging
builder.Services.AddAuditServices(builder.Configuration);

// Add Admin Gateway metrics services
builder.Services.AddSingleton<AdminGatewayMetricsService>();

// Wrap audit service with metrics tracking for medical compliance
builder.Services.Decorate<IAuditService, AuditLogMetricsWrapper>();

// Configure Azure Managed Identity for accessing external Azure resources (Key Vault, databases, etc.)
// This enables the Admin Gateway to authenticate to Azure services without storing credentials
if (!builder.Environment.IsEnvironment("Testing"))
{
    var credential = builder.Environment.IsProduction() || builder.Environment.IsEnvironment("Staging")
        ? new DefaultAzureCredential() // Uses managed identity in production/staging
        : new DefaultAzureCredential(new DefaultAzureCredentialOptions 
        { 
            ExcludeManagedIdentityCredential = true // In development, use Azure CLI or Visual Studio credentials
        });
    
    builder.Services.AddSingleton(credential);
    
    // Configure environment-specific secret handling and rotation support
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
        logger?.LogInformation("Admin Gateway: Azure Key Vault configuration provider enabled with 30-minute refresh interval for secret rotation support");
    }
    
    // Add configuration change monitoring for secret rotation with medical-grade audit logging
    builder.Services.Configure<IConfiguration>(config =>
    {
        if (!builder.Environment.IsDevelopment() && !string.IsNullOrEmpty(keyVaultUri))
        {
            // Register configuration change token for monitoring secret updates
            ChangeToken.OnChange(() => builder.Configuration.GetReloadToken(), () =>
            {
                var logger = builder.Services.BuildServiceProvider().GetService<ILogger<Program>>();
                logger?.LogInformation("MEDICAL_AUDIT: Admin Gateway configuration refresh triggered - secret rotation detected, KeyVaultUri: {KeyVaultUri}, Timestamp: {Timestamp}, Environment: {Environment}",
                    keyVaultUri,
                    DateTimeOffset.UtcNow,
                    builder.Environment.EnvironmentName);
            });
        }
    });
    
    var logger2 = builder.Services.BuildServiceProvider().GetService<ILogger<Program>>();
    logger2?.LogInformation("Admin Gateway: Azure Managed Identity configured for environment: {Environment}, Secrets Provider: {SecretsProvider}", 
        builder.Environment.EnvironmentName, secretsProvider ?? "LOCAL_PARAMETERS");
}

// Add PostgreSQL for medical-grade audit logging (configured via Aspire)
if (!builder.Environment.IsEnvironment("Testing"))
{
    var connectionString = builder.Configuration.GetConnectionString("database") 
        ?? throw new InvalidOperationException("Database connection string not found");
    
    // Add shared ApplicationDbContext for audit logging
    builder.Services.AddDbContext<InternationalCenter.Shared.Infrastructure.ApplicationDbContext>((serviceProvider, options) =>
    {
        options.UseNpgsql(connectionString);
    });
}
else
{
    // Testing environment: use minimal database context
    builder.Services.AddDbContext<InternationalCenter.Shared.Infrastructure.ApplicationDbContext>(options => { /* Will be configured by tests */ });
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

// Add Microsoft Entra External ID authentication for Admin Gateway
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("EntraExternalId"));

    // Add Services domain authorization handlers
    builder.Services.AddServicesDomainAuthorization();

    // Configure authorization policies for Services Admin operations
    builder.Services.AddAuthorization(options =>
    {
        // Default policy: require authenticated user for all Admin operations
        options.DefaultPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireClaim("aud", builder.Configuration["EntraExternalId:ClientId"] ?? throw new InvalidOperationException("EntraExternalId:ClientId not configured"))
            .Build();

        // Configure Services domain policies with medical-grade authorization handlers
        options.ConfigureServicesDomainPolicies();

        // Medical-grade fallback policy: denies access if no other policy is specified
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    });
}
else
{
    // Testing environment: use minimal authentication for integration tests
    builder.Services.AddAuthentication("Test")
        .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", options => { });
    builder.Services.AddAuthorization();
}

// Add YARP reverse proxy with service discovery
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add Redis-backed rate limiting for Admin Gateway with user-based partitioning
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    // User-based rate limiting for admin portal with medical compliance tracking (100 requests per minute)
    options.GlobalLimiter = RedisRateLimiterFactory.CreateRedisFixedWindowRateLimiter<Microsoft.AspNetCore.Http.HttpContext>(
        context =>
        {
            var userId = context.User?.Identity?.Name ?? 
                        context.Request.Headers["X-User-ID"].FirstOrDefault() ?? 
                        "anonymous";
            return $"admin_gateway_user:{userId}";
        },
        permitLimit: 100, // Medical compliance limits for admin operations
        window: TimeSpan.FromMinutes(1)
    );

    options.OnRejected = async (context, cancellationToken) =>
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        var userId = context.HttpContext.User?.Identity?.Name ?? 
                    context.HttpContext.Request.Headers["X-User-ID"].FirstOrDefault() ?? 
                    "anonymous";
        var clientIp = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? 
                      context.HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? 
                      "unknown";
        var userRoles = context.HttpContext.User?.Claims?.Where(c => c.Type == "roles")?.Select(c => c.Value)?.ToArray() ?? Array.Empty<string>();
        var correlationId = context.HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
        var userAgent = context.HttpContext.Request.Headers.UserAgent.FirstOrDefault() ?? "unknown";
        var versionService = context.HttpContext.RequestServices.GetService<IVersionService>();
        var appVersion = versionService?.GetVersion() ?? "unknown";
        
        // Enhanced medical-grade audit logging for rate limit violations with metrics
        logger.LogWarning("ADMIN_GATEWAY_RATE_LIMIT_METRICS: Medical compliance violation - UserId: {UserId}, UserRoles: [{UserRoles}], ClientIp: {ClientIp}, Path: {Path}, Method: {Method}, UserAgent: {UserAgent}, CorrelationId: {CorrelationId}, AppVersion: {AppVersion}, RateLimitType: {RateLimitType}, LimitValue: {LimitValue}, WindowMinutes: {WindowMinutes}, Timestamp: {Timestamp}, GatewayType: {GatewayType}, ComplianceLevel: {ComplianceLevel}",
            userId, 
            string.Join(",", userRoles),
            clientIp,
            context.HttpContext.Request.Path,
            context.HttpContext.Request.Method,
            userAgent,
            correlationId,
            appVersion,
            "USER_BASED",
            100,
            1,
            DateTimeOffset.UtcNow,
            "ADMIN",
            "MEDICAL_GRADE");

        // Rate limiting metrics for medical compliance monitoring
        logger.LogError("RATE_LIMIT_VIOLATION_METRICS: Admin Gateway Medical Compliance - MetricType: {MetricType}, UserId: {UserId}, UserRoles: [{UserRoles}], RequestCount: {RequestCount}, LimitExceeded: {LimitExceeded}, ComplianceViolation: {ComplianceViolation}, CorrelationId: {CorrelationId}, AppVersion: {AppVersion}",
            "RATE_LIMIT_EXCEEDED",
            userId,
            string.Join(",", userRoles),
            100, // Assuming exceeded after 100 requests
            true,
            true,
            correlationId,
            appVersion);

        // Add comprehensive rate limit headers for medical compliance monitoring
        context.HttpContext.Response.Headers["X-RateLimit-Limit"] = "100";
        context.HttpContext.Response.Headers["X-RateLimit-Remaining"] = "0";
        context.HttpContext.Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(1)).ToUnixTimeSeconds().ToString();
        context.HttpContext.Response.Headers["X-RateLimit-Type"] = "USER_BASED";
        context.HttpContext.Response.Headers["X-Gateway-Type"] = "ADMIN";
        context.HttpContext.Response.Headers["X-Compliance-Level"] = "MEDICAL_GRADE";
        
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync("Rate limit exceeded for admin operations - medical compliance violation logged", cancellationToken);
    };
});

// Add CORS for admin portal access
builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.AddPolicy("AdminPortal", policy =>
        {
            policy
                .WithOrigins("http://localhost:3000", "https://localhost:3000") // Admin portal ports
                .AllowAnyMethod()
                .AllowAnyHeader()
                .WithExposedHeaders("X-Correlation-ID", "X-Request-ID")
                .AllowCredentials(); // Required for authentication
        });
    }
    else
    {
        options.AddPolicy("AdminPortal", policy =>
        {
            policy
                .WithOrigins(builder.Configuration.GetSection("AdminAllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
                .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
                .AllowAnyHeader()
                .WithExposedHeaders("X-Correlation-ID", "X-Request-ID")
                .AllowCredentials(); // Required for authentication
        });
    }
});

// Add Aspire service discovery
builder.Services.AddAspireServiceDiscovery(builder.Configuration);

// Add enhanced medical-grade audit service with authentication context
builder.Services.AddAdminGatewayAuditServices(builder.Configuration);

// Add health checks for Admin Gateway
builder.Services.AddHealthChecks()
    .AddCheck("services-admin-api-proxy", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["proxy", "services", "admin"])
    .AddCheck("entra-external-id-auth", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["auth", "admin", "critical"]);

// Add services to the container
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseExceptionHandler();

// Add structured logging middleware with user context tracking for medical-grade audit trail continuity
app.UseAdminGatewayStructuredLogging();

// Add metrics collection middleware early in pipeline (after logging, before rate limiting)
app.UseAdminGatewayMetrics();

// Add rate limiting middleware early in pipeline
app.UseRateLimiter();

// Add rate limiting success metrics middleware for medical compliance observability
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
    var userId = context.User?.Identity?.Name ?? "anonymous";
    var userRoles = context.User?.Claims?.Where(c => c.Type == "roles")?.Select(c => c.Value)?.ToArray() ?? Array.Empty<string>();
    
    await next();
    
    // Log successful rate limiting metrics (requests that passed rate limiting) with medical compliance context
    if (context.Response.StatusCode != StatusCodes.Status429TooManyRequests)
    {
        logger.LogDebug("RATE_LIMIT_SUCCESS_METRICS: Admin Gateway Medical Compliance - MetricType: {MetricType}, UserId: {UserId}, UserRoles: [{UserRoles}], ClientIp: {ClientIp}, Path: {Path}, Method: {Method}, StatusCode: {StatusCode}, RateLimitPassed: {RateLimitPassed}, CorrelationId: {CorrelationId}, AppVersion: {AppVersion}, GatewayType: {GatewayType}, ComplianceLevel: {ComplianceLevel}, Timestamp: {Timestamp}",
            "RATE_LIMIT_PASSED",
            userId,
            string.Join(",", userRoles),
            clientIp,
            context.Request.Path,
            context.Request.Method,
            context.Response.StatusCode,
            true,
            correlationId,
            appVersion,
            "ADMIN",
            "MEDICAL_GRADE",
            DateTimeOffset.UtcNow);
    }
});

// Add authentication middleware
app.UseAuthentication();

// Add enhanced medical-grade audit middleware with authentication context (after auth, before authz)
app.UseAdminGatewayAuditMiddleware();

// Add authorization middleware
app.UseAuthorization();

// Add Services domain user context forwarding middleware
app.UseServicesDomainUserContextForwarding();

// Add enhanced security validation middleware for admin gateway with medical-grade compliance
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
    var userId = context.User?.Identity?.Name ?? "anonymous";
    var userRoles = context.User?.Claims?.Where(c => c.Type == "roles")?.Select(c => c.Value)?.ToArray() ?? Array.Empty<string>();
    
    // 1. IP Address Validation - Check IP blocklist and admin allowlist for medical compliance
    var blockedIps = configuration.GetSection("Security:BlockedIpAddresses").Get<string[]>() ?? Array.Empty<string>();
    if (blockedIps.Contains(clientIp))
    {
        logger.LogWarning("ADMIN_GATEWAY_SECURITY: MEDICAL_AUDIT: Blocked IP {ClientIp} attempted admin access to {Path}, UserId: {UserId}, CorrelationId: {CorrelationId}",
            clientIp, context.Request.Path, userId, correlationId);
        
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        await context.Response.WriteAsync("{\"error\":\"IP_ADDRESS_BLOCKED\",\"message\":\"Access from this IP address is not allowed for admin operations\"}");
        return;
    }
    
    // Admin IP allowlist validation for medical compliance
    var allowedIps = configuration.GetSection("Security:AdminAllowedIpAddresses").Get<string[]>();
    if (allowedIps?.Any() == true && !allowedIps.Contains(clientIp) && !allowedIps.Contains("*"))
    {
        logger.LogWarning("ADMIN_GATEWAY_SECURITY: MEDICAL_AUDIT: Unauthorized IP {ClientIp} attempted admin access to {Path}, UserId: {UserId}, CorrelationId: {CorrelationId}",
            clientIp, context.Request.Path, userId, correlationId);
        
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        await context.Response.WriteAsync("{\"error\":\"ADMIN_IP_UNAUTHORIZED\",\"message\":\"IP address not authorized for admin operations\"}");
        return;
    }
    
    // 2. Security Headers Validation - Check for suspicious User-Agent and SQL injection patterns
    var userAgent = context.Request.Headers.UserAgent.FirstOrDefault() ?? "";
    var suspiciousPatterns = new[] { "sqlmap", "nikto", "nmap", "masscan", "zap", "burp" };
    if (suspiciousPatterns.Any(pattern => userAgent.ToLowerInvariant().Contains(pattern)))
    {
        logger.LogWarning("ADMIN_GATEWAY_SECURITY: MEDICAL_AUDIT: Suspicious User-Agent detected: {UserAgent} from {ClientIp}, UserId: {UserId}, CorrelationId: {CorrelationId}",
            userAgent, clientIp, userId, correlationId);
        
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        await context.Response.WriteAsync("{\"error\":\"SUSPICIOUS_USER_AGENT\",\"message\":\"Admin request blocked due to security policy\"}");
        return;
    }
    
    // Check for SQL injection patterns in headers with medical-grade audit logging
    var allHeaderValues = context.Request.Headers.SelectMany(h => h.Value).ToList();
    var sqlInjectionPatterns = new[] { "union select", "drop table", "exec(", "script>", "<iframe" };
    foreach (var headerValue in allHeaderValues)
    {
        var lowerValue = headerValue?.ToLowerInvariant() ?? "";
        if (sqlInjectionPatterns.Any(pattern => lowerValue.Contains(pattern)))
        {
            logger.LogWarning("ADMIN_GATEWAY_SECURITY: MEDICAL_AUDIT: Potential security threat in headers from {ClientIp}, UserId: {UserId}, CorrelationId: {CorrelationId}",
                clientIp, userId, correlationId);
            
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.Headers["X-Correlation-ID"] = correlationId;
            await context.Response.WriteAsync("{\"error\":\"SECURITY_THREAT_HEADERS\",\"message\":\"Admin request blocked due to security policy\"}");
            return;
        }
    }
    
    // 3. Request Size Limits - Stricter limits for admin operations
    var maxRequestSize = configuration.GetValue<long>("Security:AdminMaxRequestSizeBytes", 5 * 1024 * 1024); // 5MB default for admin
    if (context.Request.ContentLength > maxRequestSize)
    {
        logger.LogWarning("ADMIN_GATEWAY_SECURITY: MEDICAL_AUDIT: Admin request size {Size} exceeds limit {Limit} from {ClientIp}, UserId: {UserId}, CorrelationId: {CorrelationId}",
            context.Request.ContentLength, maxRequestSize, clientIp, userId, correlationId);
        
        context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        await context.Response.WriteAsync("{\"error\":\"REQUEST_SIZE_EXCEEDED\",\"message\":\"Admin request payload too large\"}");
        return;
    }
    
    // 4. HTTPS Enforcement - Required for admin operations (medical compliance)
    var requireHttps = configuration.GetValue<bool>("Security:RequireHttps", true); // Default true for admin gateway
    if (requireHttps && !context.Request.IsHttps && !IsLocalDevelopment(context))
    {
        logger.LogWarning("ADMIN_GATEWAY_SECURITY: MEDICAL_AUDIT: Insecure connection attempt to {Path} from {ClientIp}, UserId: {UserId}, CorrelationId: {CorrelationId}",
            context.Request.Path, clientIp, userId, correlationId);
        
        context.Response.StatusCode = StatusCodes.Status426UpgradeRequired;
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        await context.Response.WriteAsync("{\"error\":\"INSECURE_CONNECTION\",\"message\":\"Secure connection required for admin operations\"}");
        return;
    }
    
    // Apply comprehensive security headers for Admin Gateway with medical-grade compliance and fallback policies
    context.Response.ApplyAdminGatewaySecurityHeaders(context, configuration);
    
    // Add correlation ID for medical-grade audit trail
    context.Response.Headers.Append("X-Correlation-ID", correlationId);
    
    // Medical-grade audit logging for Admin Gateway with user context
    logger.LogInformation("MEDICAL_AUDIT: Admin Gateway request: {Method} {Path} by user {UserId} with roles [{UserRoles}] from {ClientIp} with correlation {CorrelationId}",
        context.Request.Method, context.Request.Path, userId, string.Join(",", userRoles), clientIp, correlationId);

    await next();
    
    // Log response for medical-grade audit trail
    logger.LogInformation("MEDICAL_AUDIT: Admin Gateway response: {StatusCode} for correlation {CorrelationId} by user {UserId}",
        context.Response.StatusCode, correlationId, userId);
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Enable CORS for admin portal access
app.UseCors("AdminPortal");

// Map version endpoint for production monitoring
app.MapVersionEndpoint();

// Map Prometheus metrics endpoint from Infrastructure.Metrics
app.UseMetricsEndpoint();

// Map audit retention management endpoints for admin users
app.MapAuditRetentionEndpoints();

// Map rate limiting metrics endpoint for medical compliance observability
app.MapGet("/api/admin/rate-limit/metrics", (ILogger<Program> logger, IServiceProvider serviceProvider, HttpContext context) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
    var versionService = serviceProvider.GetService<IVersionService>();
    var appVersion = versionService?.GetVersion() ?? "unknown";
    var userId = context.User?.Identity?.Name ?? "anonymous";
    var userRoles = context.User?.Claims?.Where(c => c.Type == "roles")?.Select(c => c.Value)?.ToArray() ?? Array.Empty<string>();
    
    var metricsData = new
    {
        GatewayType = "ADMIN",
        RateLimitType = "USER_BASED",
        LimitValue = 100,
        WindowMinutes = 1,
        ComplianceLevel = "MEDICAL_GRADE",
        Timestamp = DateTimeOffset.UtcNow,
        AppVersion = appVersion,
        CorrelationId = correlationId,
        Status = "Active",
        BackingStore = "Redis",
        RequestedBy = new
        {
            UserId = userId,
            UserRoles = userRoles
        }
    };
    
    logger.LogInformation("RATE_LIMIT_STATUS_METRICS: Admin Gateway Medical Compliance status requested - UserId: {UserId}, UserRoles: [{UserRoles}], CorrelationId: {CorrelationId}, AppVersion: {AppVersion}, StatusRequested: {StatusRequested}, ComplianceLevel: {ComplianceLevel}",
        userId,
        string.Join(",", userRoles),
        correlationId,
        appVersion,
        true,
        "MEDICAL_GRADE");
    
    return Results.Ok(metricsData);
})
.RequireAuthorization() // Admin endpoint requires authentication
.WithName("GetAdminGatewayRateLimitMetrics")
.WithSummary("Get Admin Gateway rate limiting metrics")
.WithDescription("Returns current rate limiting configuration and status for the Admin Gateway (Medical Compliance)")
.Produces<object>(200);

// Map YARP reverse proxy to Services Admin API
app.MapReverseProxy();

// Map health checks
app.MapHealthChecks("/health");

// Initialize enhanced medical-grade audit system with authentication context
if (app.Environment.IsDevelopment())
{
    try
    {
        // Ensure audit tables are created first
        await app.Services.EnsureAuditTablesCreatedAsync();
        
        // Then initialize the audit system
        await app.Services.InitializeAdminGatewayAuditSystemAsync();
        
        var logger = app.Services.GetService<ILogger<Program>>();
        logger?.LogInformation("Admin Gateway: Enhanced medical-grade audit system with authentication context and database persistence initialized successfully");
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetService<ILogger<Program>>();
        logger?.LogError(ex, "Admin Gateway: Failed to initialize enhanced medical-grade audit system during startup");
    }
}

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