using InternationalCenter.News.Api.Data;
using InternationalCenter.News.Api.Services;
using InternationalCenter.Shared.Extensions;
using InternationalCenter.Shared.Infrastructure;

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
app.MapGrpcService<NewsGrpcService>();

// gRPC-Web support for web clients
app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });


app.MapDefaultEndpoints();

// Consumer API - Wait for migration owner (Services API) and seed news-specific data
if (app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("News API: Verifying database availability (migrations handled by Services API)...");
        
        // Only verify database connectivity - Services API handles migrations
        await context.Database.CanConnectAsync();
        
        logger.LogInformation("News API: Seeding news data...");
        
        // Seed news-specific data only
        await NewsSeedData.SeedAsync(context);
        
        logger.LogInformation("News API: Database initialization completed successfully");
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetService<ILogger<Program>>();
        logger?.LogError(ex, "News API: Failed to initialize database or seed data during startup");
    }
}

app.Run();

