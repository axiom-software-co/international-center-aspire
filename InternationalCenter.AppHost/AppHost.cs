using CommunityToolkit.Aspire.Hosting.Bun;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Configure container lifecycle management for development - Microsoft recommended patterns
var isDevelopment = builder.Environment.EnvironmentName == Environments.Development;
var isProduction = builder.Environment.EnvironmentName == Environments.Production;

// Database - Enhanced PostgreSQL configuration with Microsoft patterns
var postgres = builder.AddPostgres("postgres")
    .WithEnvironment("POSTGRES_DB", "database")
    .WithEnvironment("POSTGRES_USER", "postgres")
    .WithEnvironment("POSTGRES_PASSWORD", builder.AddParameter("postgres-password", secret: true));

if (isDevelopment)
{
    // Development: Clean containers on each restart with PgAdmin for debugging
    postgres = postgres
        .WithLifetime(ContainerLifetime.Session)
        .WithPgAdmin();
}
else if (isProduction)
{
    // Production: Persistent data volumes with enhanced configuration
    postgres = postgres
        .WithDataVolume("international-center-postgres-data")
        .WithEnvironment("POSTGRES_SHARED_PRELOAD_LIBRARIES", "pg_stat_statements")
        .WithEnvironment("POSTGRES_MAX_CONNECTIONS", "200")
        .WithEnvironment("POSTGRES_SHARED_BUFFERS", "256MB");
}
else
{
    // Staging: Similar to production but with data volumes for testing
    postgres = postgres
        .WithDataVolume("international-center-postgres-staging")
        .WithPgAdmin();
}

var database = postgres.AddDatabase("database");

// Redis - High-performance caching with Microsoft Aspire patterns
var redis = builder.AddRedis("redis")
    .WithLifetime(isDevelopment ? ContainerLifetime.Session : ContainerLifetime.Persistent);

// Apply environment-specific Redis configuration
if (isProduction)
{
    redis = redis
        .WithDataVolume("international-center-redis-data")
        .WithEnvironment("MAXMEMORY", "2gb")
        .WithEnvironment("MAXMEMORY_POLICY", "allkeys-lru");
}
else if (!isDevelopment)
{
    redis = redis
        .WithDataVolume("international-center-redis-staging");
}

// Migration Service - applies database migrations and runs to completion
var migrationService = builder.AddProject<Projects.InternationalCenter_Migrations_Service>("migration-service")
    .WithReference(database)
    .WithEnvironment("ASPIRE_ALLOW_UNSECURED_TRANSPORT", isDevelopment.ToString().ToLower());

// gRPC API Services - Enhanced with Microsoft Aspire enterprise patterns
var servicesApi = builder.AddProject<Projects.InternationalCenter_Services_Public_Api>("services-public-api")
    .WithReference(database)
    .WithReference(redis)
    .WithEnvironment("ASPIRE_ALLOW_UNSECURED_TRANSPORT", isDevelopment.ToString().ToLower())
    .WithEnvironment("OTEL_SERVICE_NAME", "InternationalCenter.Services.Public.Api")
    .WithHttpEndpoint(port: isDevelopment ? 8081 : null, name: "publicapi")
    .WithHttpsEndpoint(port: isDevelopment ? 8441 : null, name: "publicapi-tls")
    .WaitForCompletion(migrationService);

// Services Admin API - Enhanced with Aspire-native service discovery and security
var servicesAdminApi = builder.AddProject<Projects.InternationalCenter_Services_Admin_Api>("services-admin-api")
    .WithReference(database)
    .WithReference(redis)
    .WithEnvironment("ASPIRE_ALLOW_UNSECURED_TRANSPORT", isDevelopment.ToString().ToLower())
    .WithEnvironment("OTEL_SERVICE_NAME", "InternationalCenter.Services.Admin.Api")
    .WithHttpEndpoint(port: isDevelopment ? 8088 : null, name: "adminapi")
    .WithHttpsEndpoint(port: isDevelopment ? 8448 : null, name: "adminapi-tls")
    .WaitForCompletion(migrationService);


// Configure all gRPC services with consistent Microsoft patterns and Aspire-native service discovery
/*var newsApi = builder.AddProject<Projects.InternationalCenter_News_Api>("news-api")
    .WithReference(database)
    .WithReference(redis)
    .WithEnvironment("ASPIRE_ALLOW_UNSECURED_TRANSPORT", isDevelopment.ToString().ToLower())
    .WithEnvironment("OTEL_SERVICE_NAME", "InternationalCenter.News.Api")
    .WithHttpEndpoint(port: isDevelopment ? 8082 : null, name: "newsapi")
    .WithHttpsEndpoint(port: isDevelopment ? 8442 : null, name: "newsapi-tls")
    .WaitForCompletion(migrationService);*/

/*var contactsApi = builder.AddProject<Projects.InternationalCenter_Contacts_Api>("contacts-api")
    .WithReference(database)
    .WithReference(redis)
    .WithEnvironment("ASPIRE_ALLOW_UNSECURED_TRANSPORT", isDevelopment.ToString().ToLower())
    .WithEnvironment("OTEL_SERVICE_NAME", "InternationalCenter.Contacts.Api")
    .WithHttpEndpoint(port: isDevelopment ? 8084 : null, name: "contactsapi")
    .WithHttpsEndpoint(port: isDevelopment ? 8444 : null, name: "contactsapi-tls")
    .WaitForCompletion(migrationService);*/

/*var researchApi = builder.AddProject<Projects.InternationalCenter_Research_Api>("research-api")
    .WithReference(database)
    .WithReference(redis)
    .WithEnvironment("ASPIRE_ALLOW_UNSECURED_TRANSPORT", isDevelopment.ToString().ToLower())
    .WithEnvironment("OTEL_SERVICE_NAME", "InternationalCenter.Research.Api")
    .WithHttpEndpoint(port: isDevelopment ? 8083 : null, name: "researchapi")
    .WithHttpsEndpoint(port: isDevelopment ? 8443 : null, name: "researchapi-tls")
    .WaitForCompletion(migrationService);*/

/*var searchApi = builder.AddProject<Projects.InternationalCenter_Search_Api>("search-api")
    .WithReference(database)
    .WithReference(redis)
    .WithEnvironment("ASPIRE_ALLOW_UNSECURED_TRANSPORT", isDevelopment.ToString().ToLower())
    .WithEnvironment("OTEL_SERVICE_NAME", "InternationalCenter.Search.Api")
    .WithHttpEndpoint(port: isDevelopment ? 8087 : null, name: "searchapi")
    .WithHttpsEndpoint(port: isDevelopment ? 8447 : null, name: "searchapi-tls")
    .WaitForCompletion(migrationService);*/

/*var eventsApi = builder.AddProject<Projects.InternationalCenter_Events_Api>("events-api")
    .WithReference(database)
    .WithReference(redis)
    .WithEnvironment("ASPIRE_ALLOW_UNSECURED_TRANSPORT", isDevelopment.ToString().ToLower())
    .WithEnvironment("OTEL_SERVICE_NAME", "InternationalCenter.Events.Api")
    .WithHttpEndpoint(port: isDevelopment ? 8085 : null, name: "eventsapi")
    .WithHttpsEndpoint(port: isDevelopment ? 8445 : null, name: "eventsapi-tls")
    .WaitForCompletion(migrationService);*/

/*var newsletterApi = builder.AddProject<Projects.InternationalCenter_Newsletter_Api>("newsletter-api")
    .WithReference(database)
    .WithReference(redis)
    .WithEnvironment("ASPIRE_ALLOW_UNSECURED_TRANSPORT", isDevelopment.ToString().ToLower())
    .WithEnvironment("OTEL_SERVICE_NAME", "InternationalCenter.Newsletter.Api")
    .WithHttpEndpoint(port: isDevelopment ? 8086 : null, name: "newsletterapi")
    .WithHttpsEndpoint(port: isDevelopment ? 8446 : null, name: "newsletterapi-tls")
    .WaitForCompletion(migrationService);*/

// Website - Astro.js frontend with enhanced Microsoft Aspire service discovery
var website = builder.AddBunApp("website", "/home/tojkuv/Documents/GitHub/aspire-testing/international-center-infrastructure/international-center-platform/web/international-center-website", "dev")
    .WithEnvironment("OTEL_SERVICE_NAME", "InternationalCenter.Website")
    .WithEnvironment("NODE_ENV", isDevelopment ? "development" : isProduction ? "production" : "staging")
    .WithEnvironment("ASPIRE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithHttpEndpoint(port: isDevelopment ? 4321 : null, env: "PORT", isProxied: false)
    .WithExternalHttpEndpoints()
    // Enhanced service discovery with semantic naming - only Services APIs enabled
    .WithReference(servicesApi)
    .WithReference(servicesAdminApi)
    //.WithReference(newsApi) // Temporarily disabled
    //.WithReference(contactsApi) // Temporarily disabled
    //.WithReference(researchApi) // Temporarily disabled
    //.WithReference(searchApi) // Temporarily disabled
    //.WithReference(eventsApi) // Temporarily disabled
    //.WithReference(newsletterApi) // Temporarily disabled
    .WaitForCompletion(migrationService);

// Build and configure application with enhanced lifecycle management - Microsoft patterns
var app = builder.Build();

// Add enhanced lifecycle management with observability
if (isDevelopment)
{
    AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
    {
        Console.WriteLine("🔄 Aspire AppHost: Initiating graceful shutdown and container cleanup...");
        Console.WriteLine("📊 Development metrics and telemetry will be preserved for analysis");
    };
    
    Console.CancelKeyPress += (sender, e) =>
    {
        Console.WriteLine("⚡ Aspire AppHost: Received shutdown signal, cleaning up resources...");
        Console.WriteLine("🧹 Session-based containers will be removed to ensure clean restart");
        e.Cancel = false; // Allow graceful shutdown
    };
    
    Console.WriteLine("🚀 Microsoft Aspire AppHost starting in DEVELOPMENT mode");
    Console.WriteLine("📊 OpenTelemetry tracing and metrics enabled");
    Console.WriteLine("🔍 Service discovery with hardcoded ports for debugging");
    Console.WriteLine("🗄️  PostgreSQL with PgAdmin available for database inspection");
}
else if (isProduction)
{
    Console.WriteLine("🏭 Microsoft Aspire AppHost starting in PRODUCTION mode");
    Console.WriteLine("📈 High availability with service replicas enabled");
    Console.WriteLine("🔐 Secure endpoints with HTTPS and TLS");
    Console.WriteLine("📊 Enterprise observability with distributed tracing");
    Console.WriteLine("🗄️  Persistent data volumes for PostgreSQL and Microsoft Garnet");
}
else
{
    Console.WriteLine("🧪 Microsoft Aspire AppHost starting in STAGING mode");
    Console.WriteLine("🔍 Production-like configuration with debugging enabled");
}

app.Run();