using Microsoft.AspNetCore.SpaServices.Extensions;
using Microsoft.Extensions.FileProviders;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Configure services for Website hosting
builder.Services.AddSpaStaticFiles(configuration =>
{
    configuration.RootPath = "dist";
});

// Configure development services
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });
}

var app = builder.Build();

// Configure request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseCors();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Force HTTPS in production (medical-grade security)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Static file serving configuration
app.UseStaticFiles();

if (!app.Environment.IsDevelopment())
{
    // Production: Serve Astro-built static files
    app.UseSpaStaticFiles();
}

// Health check endpoint for Aspire monitoring
app.MapGet("/health", () => Results.Ok(new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName,
    version = typeof(Program).Assembly.GetName().Version?.ToString(),
    frontend = "Astro + Vue + Tailwind + shadcn-vue + Pinia"
}));

// API version endpoint for medical-grade compliance
app.MapGet("/api/version", () => Results.Ok(new
{
    version = typeof(Program).Assembly.GetName().Version?.ToString(),
    environment = app.Environment.EnvironmentName,
    buildDate = File.GetCreationTime(typeof(Program).Assembly.Location),
    frontend = new
    {
        framework = "Astro",
        uiLibrary = "Vue 3",
        styling = "Tailwind CSS + shadcn-vue",
        stateManagement = "Pinia",
        runtime = "Bun"
    }
}));

// SPA configuration based on environment
app.UseSpa(spa =>
{
    spa.Options.SourcePath = ".";
    spa.Options.DefaultPageStaticFileOptions = new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.ContentRootPath, "dist"))
    };

    if (app.Environment.IsDevelopment())
    {
        // Development: Proxy to Astro dev server for hot reload
        spa.UseProxyToSpaDevelopmentServer("http://localhost:4321");
    }
    else
    {
        // Production: Serve pre-built static files
        spa.Options.DefaultPage = "/index.html";
    }
});

// Configure structured logging for medical-grade audit compliance
app.Logger.LogInformation("🏥 International Center Website starting in {Environment} mode", app.Environment.EnvironmentName);

if (app.Environment.IsDevelopment())
{
    app.Logger.LogInformation("🔥 Development mode: Proxying to Astro dev server at http://localhost:4321");
    app.Logger.LogInformation("💡 For frontend development, run 'bun run dev' in parallel");
    app.Logger.LogInformation("🧪 CORS enabled for development testing");
}
else if (app.Environment.IsProduction())
{
    app.Logger.LogInformation("🚀 Production mode: Serving optimized static assets");
    app.Logger.LogInformation("🔐 HTTPS redirection and HSTS enabled for medical-grade security");
    app.Logger.LogInformation("📊 Health checks available at /health for monitoring");
}

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "❌ Website application failed to start: {Error}", ex.Message);
    throw;
}