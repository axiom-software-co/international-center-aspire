using InternationalCenter.Shared.Extensions;
using InternationalCenter.Shared.Infrastructure;
using InternationalCenter.Shared.Models;
using InternationalCenter.Search.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add PostgreSQL DbContext with Aspire client integration (Microsoft recommended pattern)
builder.AddNpgsqlDbContext<ApplicationDbContext>("database");

// Add shared infrastructure (without database context)
builder.Services.AddSharedInfrastructure(builder.Configuration);

// Add gRPC services
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

// gRPC-Web will be configured in the request pipeline

// Add CORS for gRPC-Web
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
    });
});

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

// Enable CORS
app.UseCors("AllowAll");

// Enable gRPC-Web
app.UseGrpcWeb();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Map gRPC services
app.MapGrpcService<SearchGrpcService>().EnableGrpcWeb();


app.MapDefaultEndpoints();

// Consumer API - Wait for migration owner (Services API) for database readiness
if (app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("Search API: Verifying database availability (migrations handled by Services API)...");
        
        // Only verify database connectivity - Services API handles migrations
        await context.Database.CanConnectAsync();
        
        logger.LogInformation("Search API: Database initialization completed successfully");
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetService<ILogger<Program>>();
        logger?.LogError(ex, "Search API: Failed to initialize database during startup");
    }
}

app.Run();