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


app.MapDefaultEndpoints();

// Consumer API - Wait for migration owner (Services API) for database readiness
if (app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("Newsletter API: Verifying database availability (migrations handled by Services API)...");
        
        // Only verify database connectivity - Services API handles migrations
        await context.Database.CanConnectAsync();
        
        logger.LogInformation("Newsletter API: Database initialization completed successfully");
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetService<ILogger<Program>>();
        logger?.LogError(ex, "Newsletter API: Failed to initialize database during startup");
    }
}

app.Run();