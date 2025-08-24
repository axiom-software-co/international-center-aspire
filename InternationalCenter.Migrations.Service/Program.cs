using InternationalCenter.Migrations.Service;
using InternationalCenter.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

// Add service defaults & Aspire client integrations
builder.AddServiceDefaults();

// Add PostgreSQL DbContext with Aspire client integration (Microsoft recommended pattern)
// Configure migrations assembly to this project where migrations are located
builder.AddNpgsqlDbContext<ApplicationDbContext>("database", configureDbContextOptions: options =>
{
    options.UseNpgsql(npgsqlOptions =>
    {
        // CRITICAL: Specify that migrations are in this assembly (Migration Service)
        // This aligns with Microsoft's recommended pattern for centralized migrations
        npgsqlOptions.MigrationsAssembly(typeof(Program).Assembly.GetName().Name);
    });
});

// Add the migration worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();