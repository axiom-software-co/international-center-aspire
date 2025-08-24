using InternationalCenter.Contacts.Api.Services;
using InternationalCenter.Shared.Extensions;
using InternationalCenter.Shared.Infrastructure;
using InternationalCenter.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;

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

// Configure gRPC services
app.MapGrpcService<ContactsGrpcService>();

// Enable gRPC-Web for browser compatibility
app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });

app.MapDefaultEndpoints();

// Consumer API - Wait for migration owner (Services API) for database readiness
if (app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("Contacts API: Verifying database availability (migrations handled by Services API)...");
        
        // Only verify database connectivity - Services API handles migrations
        await context.Database.CanConnectAsync();
        
        logger.LogInformation("Contacts API: Database initialization completed successfully");
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetService<ILogger<Program>>();
        logger?.LogError(ex, "Contacts API: Failed to initialize database during startup");
    }
}


app.Run();