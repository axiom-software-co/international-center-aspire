using InternationalCenter.Services.Public.Api.Infrastructure.Extensions;
using InternationalCenter.Services.Domain.Infrastructure.Data;
using InternationalCenter.Services.Domain.Infrastructure.Extensions;
using InternationalCenter.Services.Public.Api.Extensions;
using Services.Public.Api.Infrastructure.Services;
using Services.Public.Api.Infrastructure.Middleware;
using Infrastructure.Metrics.Extensions;
using InternationalCenter.Shared.Infrastructure.Observability;
using InternationalCenter.Shared.Infrastructure.Performance;
using InternationalCenter.Shared.Extensions;
using InternationalCenter.Shared.Configuration;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to remove server header for security
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
});

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add Infrastructure.Metrics for Prometheus metrics collection
builder.Services.AddMetricsInfrastructure(builder.Configuration);

// Add Services Public API metrics services
builder.Services.AddSingleton<ServicesPublicApiMetricsService>();
builder.Services.AddScoped<DapperMetricsWrapper>(serviceProvider =>
{
    var metricsService = serviceProvider.GetRequiredService<ServicesPublicApiMetricsService>();
    var logger = serviceProvider.GetRequiredService<ILogger<DapperMetricsWrapper>>();
    var connectionString = builder.Configuration.GetConnectionString("database") ?? throw new InvalidOperationException("Database connection string not found");
    return new DapperMetricsWrapper(metricsService, logger, connectionString);
});

// Add Services-specific DbContext with Aspire integration (Clean Architecture)
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
    
    // Register unified ServicesDbContext with medical-grade audit capabilities
    builder.Services.AddServicesDbContext(connectionString);
    
    // CRITICAL: Register ApplicationDbContext for migration services
    // Migration services (MigrationAuditService, MigrationOrchestrationService, etc.) depend on ApplicationDbContext
    builder.Services.AddDbContext<InternationalCenter.Shared.Infrastructure.ApplicationDbContext>(options =>
    {
        options.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsAssembly("InternationalCenter.Migrations.Service");
        });
        
        // Microsoft recommended optimizations
        options.EnableDetailedErrors();
        options.EnableServiceProviderCaching();
        options.EnableSensitiveDataLogging(false); // Never in production
    });
}

// Add Redis distributed cache (configured via Aspire)
// Re-enabled after PostgreSQL orchestration and API separation are stable
var redisConnectionString = builder.Configuration.GetConnectionString("redis");
if (!string.IsNullOrEmpty(redisConnectionString))
{
    try
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            // Aspire will provide the connection string to Redis
            options.Configuration = redisConnectionString;
        });

        // Add Redis connection multiplexer for services that need direct access (Redis-compatible)
        builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(serviceProvider =>
        {
            try
            {
                return StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnectionString);
            }
            catch (Exception ex)
            {
                var logger = serviceProvider.GetService<ILogger<Program>>();
                logger?.LogWarning(ex, "Services API: Failed to connect to Redis during startup, falling back to memory cache");
                // Don't throw - let the service start without Redis
                return null!; // This will be handled by using IMemoryCache instead
            }
        });
    }
    catch (Exception ex)
    {
        var logger = builder.Services.BuildServiceProvider().GetService<ILogger<Program>>();
        logger?.LogWarning(ex, "Services API: Redis setup failed, using in-memory cache for development");
        builder.Services.AddMemoryCache();
    }
}
else
{
    // Development fallback - use in-memory caching when Redis is not available
    builder.Services.AddMemoryCache();
    var logger = builder.Services.BuildServiceProvider().GetService<ILogger<Program>>();
    logger?.LogWarning("Services API: Redis connection string not found, using in-memory cache for development");
}

// Add Aspire-native service discovery and health checks
builder.Services.AddAspireServiceDiscovery(builder.Configuration);

// Add performance optimizations (Microsoft recommended patterns)  
// Skip cache service when Redis is not available to prevent DI failures
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddPerformanceOptimizations(builder.Environment);
}
else
{
    // Add minimal performance optimizations without Redis-dependent cache services
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
        
        options.MimeTypes = new[]
        {
            "text/plain",
            "text/html", 
            "text/css",
            "text/javascript",
            "application/javascript",
            "application/json",
            "application/xml",
            "text/xml"
        };
    });
    
    builder.Services.AddResponseCaching();
    
    // Add output caching with in-memory store (required for middleware)
    builder.Services.AddOutputCache(options =>
    {
        options.AddBasePolicy(policy => policy.Expire(TimeSpan.FromMinutes(5)));
    });
    
    // Add memory cache as fallback
    builder.Services.AddMemoryCache();
    
    // Add critical cache services that handlers depend on
    builder.Services.AddSingleton<InternationalCenter.Shared.Infrastructure.Caching.ICacheKeyService, InternationalCenter.Shared.Infrastructure.Caching.CacheKeyService>();
    
    // Note: ICacheService is not registered when Redis is unavailable
    // Handlers should gracefully handle the missing dependency
}

// Add production observability (Microsoft recommended patterns)
builder.Services.AddProductionObservability<ServicesDbContext>("Services.Api");

// Add CORS for REST API with environment-specific configuration
builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.AddPolicy("Development", policy =>
        {
            policy
                .WithOrigins("http://localhost:4321", "https://localhost:4321")
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
                .WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
                .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
                .AllowAnyHeader()
                .WithExposedHeaders("X-Correlation-ID", "X-Request-ID");
        });
    }
});

// Add FluentValidation with security-focused validation for public API
builder.Services.AddMedicalGradeValidation();
builder.Services.AddFluentValidationFromAssembly(typeof(Program).Assembly);

// Security now handled at the Public Gateway level for better separation of concerns

// Add clean architecture services following Microsoft patterns
builder.Services.AddDomainServices();
builder.Services.AddApplicationServices();

// Add Services Public API specific health checks
builder.Services.AddPublicHealthChecks();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

// Security middleware now handled at the Public Gateway level

// Add Services Public API metrics collection middleware
app.UseServicesPublicApiMetrics();

// Add performance optimizations middleware (Microsoft recommended patterns)
app.UsePerformanceOptimizations(builder.Environment);

// Add production observability middleware (Microsoft recommended patterns)
app.UseProductionObservability("Services.Api");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Enable CORS for REST API with environment-specific policy
app.UseCors(app.Environment.IsDevelopment() ? "Development" : "Production");

// Add authentication and authorization for zero-trust security
app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapDefaultEndpoints();

// Map version endpoint for production monitoring
app.MapVersionEndpoint();

// Map Prometheus metrics endpoint from Infrastructure.Metrics
app.UseMetricsEndpoint();

// Configure REST API endpoints (minimal APIs) - replaced gRPC services
app.MapServicesRestApi();

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
        
        // Validate service discovery configuration (disabled during API isolation testing)
        /*var serviceDiscovery = scope.ServiceProvider.GetService<ServiceDiscoveryConfiguration>();
        if (serviceDiscovery != null)
        {
            serviceDiscovery.ValidateServiceDiscovery();
        }*/
        logger.LogInformation("Services API: Service discovery validation skipped (API isolation mode)");
        
        logger.LogInformation("Services API: Verifying database availability (migrations handled by Migration Service)...");
        
        await context.Database.CanConnectAsync();
        logger.LogInformation("Services API: Database connection successful");
        
        var existingCount = await context.Services.CountAsync();
        logger.LogInformation("Services API: Clean architecture initialized. Total services: {ExistingCount}", existingCount);
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetService<ILogger<Program>>();
        logger?.LogError(ex, "Services API: Failed to initialize database during startup");
    }
}

app.Run();