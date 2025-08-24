using InternationalCenter.Events.Api.Services;
using InternationalCenter.Shared.Extensions;
using InternationalCenter.Shared.Infrastructure;
using InternationalCenter.Shared.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add PostgreSQL DbContext with Aspire client integration (Microsoft recommended pattern)
builder.AddNpgsqlDbContext<ApplicationDbContext>("database");

// Add shared infrastructure (without database context)
builder.Services.AddSharedInfrastructure(builder.Configuration);

// Add CORS for gRPC-Web
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
    });
});

// Add gRPC services
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaxReceiveMessageSize = 4 * 1024 * 1024; // 4MB
    options.MaxSendMessageSize = 4 * 1024 * 1024; // 4MB
});

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Enable CORS before gRPC-Web
app.UseCors("AllowAll");

// Enable gRPC-Web for browser compatibility
app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });

// Configure gRPC services with explicit gRPC-Web enablement
app.MapGrpcService<EventsGrpcService>().EnableGrpcWeb();

app.MapDefaultEndpoints();

// Simple test endpoint
app.MapGet("/test", () => "Events API is working!");

// Debug endpoint to check database status
app.MapGet("/debug/db-status", async (ApplicationDbContext context) =>
{
    try
    {
        var eventsCount = await context.Events.CountAsync();
        var registrationsCount = await context.EventRegistrations.CountAsync();
        
        return Results.Ok(new { 
            eventsCount, 
            registrationsCount,
            canConnect = await context.Database.CanConnectAsync(),
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Database error: {ex.Message}");
    }
});

// Temporary JSON API for frontend integration (to be replaced with gRPC-Web later)
app.MapGet("/api/events", async (ApplicationDbContext context, int page = 1, int pageSize = 20) =>
{
    try
    {
        var query = context.Events
            .Where(e => e.Status == "published")
            .OrderBy(e => e.StartDate);

        var total = await query.CountAsync();
        var events = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.Slug,
                e.Description,
                e.StartDate,
                e.EndDate,
                e.Location,
                e.IsVirtual,
                e.Featured,
                e.Category,
                e.IsFree,
                e.Price,
                e.Currency,
                e.ImageUrl,
                CreatedAt = e.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                UpdatedAt = e.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            })
            .ToListAsync();

        return Results.Ok(new
        {
            events,
            pagination = new
            {
                page,
                pageSize,
                total,
                totalPages = (int)Math.Ceiling((double)total / pageSize)
            }
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error retrieving events: {ex.Message}");
    }
});

app.MapGet("/api/events/featured", async (ApplicationDbContext context, int limit = 5) =>
{
    try
    {
        var events = await context.Events
            .Where(e => e.Status == "published" && e.Featured)
            .OrderBy(e => e.StartDate)
            .Take(limit)
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.Slug,
                e.Description,
                e.StartDate,
                e.EndDate,
                e.Location,
                e.IsVirtual,
                e.ImageUrl
            })
            .ToListAsync();

        return Results.Ok(new { events });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error retrieving featured events: {ex.Message}");
    }
});

// Consumer API - Wait for migration owner (Services API) for database readiness
if (app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("Events API: Verifying database availability (migrations handled by Services API)...");
        
        // Only verify database connectivity - Services API handles migrations
        await context.Database.CanConnectAsync();
        
        logger.LogInformation("Events API: Database initialization completed successfully");
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetService<ILogger<Program>>();
        logger?.LogError(ex, "Events API: Failed to initialize database during startup");
    }
}

app.Run();