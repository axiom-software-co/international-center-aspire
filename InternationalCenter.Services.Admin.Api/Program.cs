using InternationalCenter.Services.Admin.Api.Infrastructure.Extensions;
using InternationalCenter.Services.Domain.Infrastructure.Data;
using InternationalCenter.Shared.Infrastructure.Observability;
using InternationalCenter.Shared.Infrastructure.Performance;
using InternationalCenter.Shared.Extensions;
using InternationalCenter.Shared.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.Services.AddServiceDefaults();

// Add Services Admin DbContext with Aspire integration (shared with public API)
// Handle testing environments more gracefully - tests will provide connection string via WebApplicationFactory
if (builder.Environment.IsEnvironment("Testing"))
{
    // In testing, the WebApplicationFactory will provide the connection string
    // We'll defer DbContext registration until the connection string is available
    builder.Services.AddDbContext<ServicesDbContext>(options => { /* Will be configured by tests */ });
    
    // CRITICAL: Register ApplicationDbContext for migration services in testing environment
    // Migration services depend on this context and will fail without it
    builder.Services.AddDbContext<InternationalCenter.Shared.Infrastructure.ApplicationDbContext>(options => 
    { 
        /* Will be configured by tests to use same connection string as ServicesDbContext */ 
    });
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("database") 
        ?? throw new InvalidOperationException("Database connection string not found");
    builder.Services.AddServicesDbContext(connectionString);
}

// Add Microsoft Garnet distributed cache (Redis-compatible, configured via Aspire)
// Re-enabled after PostgreSQL orchestration and API separation are stable
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        // Aspire will provide the connection string
        options.Configuration = builder.Configuration.GetConnectionString("redis");
    });
}
else
{
    // In testing, WebApplicationFactory will configure Redis
    builder.Services.AddStackExchangeRedisCache(options => { /* Will be configured by tests */ });
}

// Add Aspire-native service discovery and health checks
builder.Services.AddAspireServiceDiscovery(builder.Configuration);

// Add performance optimizations (Microsoft recommended patterns)
builder.Services.AddPerformanceOptimizations(builder.Environment);

// Add production observability (Microsoft recommended patterns)
builder.Services.AddProductionObservability<ServicesDbContext>("Services.Admin.Api");

// Add CORS for REST API with environment-specific configuration (more restrictive for admin)
builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.AddPolicy("Development", policy =>
        {
            policy
                .WithOrigins("http://localhost:3000", "https://localhost:3000") // Admin portal ports
                .AllowAnyMethod()
                .AllowAnyHeader()
                .WithExposedHeaders("X-Correlation-ID", "X-Request-ID");
        });
    }
    else
    {
        options.AddPolicy("Production", policy =>
        {
            policy
                .WithOrigins(builder.Configuration.GetSection("AdminAllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
                .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
                .AllowAnyHeader()
                .WithExposedHeaders("X-Correlation-ID", "X-Request-ID");
        });
    }
});


// Add admin-specific services following Microsoft patterns
builder.Services.AddAdminDomainServices();
builder.Services.AddAdminApplicationServices();

// Add authentication and authorization for medical-grade admin security
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddAuthentication()
        .AddBearerToken(); // Simple bearer token auth for production

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminAccess", policy => 
            policy.RequireAuthenticatedUser()); // Basic authenticated user requirement
    });
}
else
{
    // In testing environment, allow anonymous access for integration tests
    builder.Services.AddAuthentication();
    builder.Services.AddAuthorization(options =>
    {
        options.DefaultPolicy = new AuthorizationPolicyBuilder()
            .RequireAssertion(_ => true) // Allow all requests in testing
            .Build();
    });
}

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

// Add performance optimizations middleware (Microsoft recommended patterns)
app.UsePerformanceOptimizations(builder.Environment);

// Add production observability middleware (Microsoft recommended patterns)
app.UseProductionObservability("Services.Admin.Api");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Enable CORS for Admin REST API with environment-specific policy (admin-restricted)
app.UseCors(app.Environment.IsDevelopment() ? "Development" : "Production");

// Add routing and authorization middleware for REST endpoints
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Configure Admin REST API endpoints (replaced gRPC handlers)
app.MapAdminServiceEndpoints();

// Initialize database connectivity check for development environment only
if (app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ServicesDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        // Log Aspire environment information
        logger.LogAspireEnvironmentInfo();
        
        // Validate service discovery configuration
        var serviceDiscovery = scope.ServiceProvider.GetService<ServiceDiscoveryConfiguration>();
        if (serviceDiscovery != null)
        {
            serviceDiscovery.ValidateServiceDiscovery();
        }
        
        logger.LogInformation("Services Admin API: Verifying database availability (shared with public API)...");
        
        await context.Database.CanConnectAsync();
        logger.LogInformation("Services Admin API: Database connection successful");
        
        var existingCount = await context.Services.CountAsync();
        logger.LogInformation("Services Admin API: Admin access initialized. Total services: {ExistingCount}", existingCount);
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetService<ILogger<Program>>();
        logger?.LogError(ex, "Services Admin API: Failed to initialize database during startup");
    }
}

app.Run();

// Make Program class accessible for integration testing (Microsoft recommended pattern)
public partial class Program { }