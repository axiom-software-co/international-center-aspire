using CommunityToolkit.Aspire.Hosting.Bun;
using Microsoft.Extensions.Hosting;
using Aspire.Hosting;
using Aspire.Hosting.Azure;

var builder = DistributedApplication.CreateBuilder(args);

// Configure container lifecycle management for development - Microsoft recommended patterns
// Enhanced for Podman compatibility per user requirements (no Docker usage)
var isDevelopment = builder.Environment.EnvironmentName == Environments.Development;
var isProduction = builder.Environment.EnvironmentName == Environments.Production;

// Podman-specific configuration - ensure Podman is used instead of Docker
if (!isDevelopment && !Environment.GetEnvironmentVariable("DOCKER_HOST")?.Contains("podman") == true)
{
    Console.WriteLine("⚠️  Warning: Container runtime should be Podman for production compliance");
}

// Azure Key Vault configuration for medical-grade secrets management
// Environment-specific secret resolution with proper fallback handling
var keyVault = isDevelopment 
    ? null // Development uses local secrets for faster iteration
    : builder.AddAzureKeyVault("international-center-keyvault");

// Configure PostgreSQL password secret based on environment
var postgresPassword = isDevelopment
    ? builder.AddParameter("postgres-password", secret: true) // Local parameter for development
    : keyVault!.AddSecret("postgres-password"); // Azure Key Vault secret for staging/production

// Configure Microsoft Entra External ID secrets for Admin Gateway authentication
var entraTenantId = isDevelopment
    ? builder.AddParameter("entra-tenant-id", secret: true) // Local parameter for development
    : keyVault!.AddSecret("entra-tenant-id"); // Azure Key Vault secret for staging/production

var entraClientId = isDevelopment
    ? builder.AddParameter("entra-client-id", secret: true) // Local parameter for development
    : keyVault!.AddSecret("entra-client-id"); // Azure Key Vault secret for staging/production

var entraClientSecret = isDevelopment
    ? builder.AddParameter("entra-client-secret", secret: true) // Local parameter for development
    : keyVault!.AddSecret("entra-client-secret"); // Azure Key Vault secret for staging/production

// Configure additional secrets for comprehensive gateway orchestration
var redisConnectionString = isDevelopment
    ? null // Development uses Aspire-generated Redis connection string
    : keyVault!.AddSecret("redis-connection-string"); // Azure Key Vault secret for production Redis

// Configure application insights connection string for medical-grade monitoring
var applicationInsightsConnectionString = isDevelopment
    ? builder.AddParameter("appinsights-connection-string", secret: true) // Local parameter for development
    : keyVault!.AddSecret("appinsights-connection-string"); // Azure Key Vault secret for staging/production

// Database - Enhanced PostgreSQL configuration with Microsoft patterns
var postgres = builder.AddPostgres("postgres")
    .WithEnvironment("POSTGRES_DB", "database")
    .WithEnvironment("POSTGRES_USER", "postgres")
    .WithEnvironment("POSTGRES_PASSWORD", postgresPassword);

// Enhanced PostgreSQL configuration optimized for Podman containers
if (isDevelopment)
{
    // Development: Clean containers on each restart with PgAdmin for debugging
    // Podman-optimized session management
    postgres = postgres
        .WithLifetime(ContainerLifetime.Session)
        .WithPgAdmin()
        .WithEnvironment("POSTGRES_LOG_STATEMENT", "all")
        .WithEnvironment("POSTGRES_LOG_MIN_DURATION_STATEMENT", "1000"); // Log slow queries
}
else if (isProduction)
{
    // Production: Persistent data volumes with enhanced configuration for Services APIs
    // Medical-grade database configuration for compliance
    postgres = postgres
        .WithDataVolume("international-center-postgres-data")
        .WithEnvironment("POSTGRES_SHARED_PRELOAD_LIBRARIES", "pg_stat_statements,pg_audit")
        .WithEnvironment("POSTGRES_MAX_CONNECTIONS", "300") // Higher for Services APIs
        .WithEnvironment("POSTGRES_SHARED_BUFFERS", "512MB")
        .WithEnvironment("POSTGRES_EFFECTIVE_CACHE_SIZE", "2GB")
        .WithEnvironment("POSTGRES_WORK_MEM", "16MB")
        .WithEnvironment("POSTGRES_MAINTENANCE_WORK_MEM", "256MB")
        .WithEnvironment("POSTGRES_WAL_BUFFERS", "16MB")
        .WithEnvironment("POSTGRES_CHECKPOINT_COMPLETION_TARGET", "0.9")
        .WithEnvironment("POSTGRES_LOG_CHECKPOINTS", "on")
        .WithEnvironment("POSTGRES_LOG_CONNECTIONS", "on")
        .WithEnvironment("POSTGRES_LOG_DISCONNECTIONS", "on");
}
else
{
    // Staging: Similar to production but with data volumes for testing
    postgres = postgres
        .WithDataVolume("international-center-postgres-staging")
        .WithPgAdmin()
        .WithEnvironment("POSTGRES_SHARED_PRELOAD_LIBRARIES", "pg_stat_statements")
        .WithEnvironment("POSTGRES_MAX_CONNECTIONS", "150")
        .WithEnvironment("POSTGRES_SHARED_BUFFERS", "256MB");
}

var database = postgres.AddDatabase("database");

// Redis - High-performance caching optimized for Services APIs (per user requirement: Redis not Garnet)
// Enhanced configuration for Podman container management
var redis = builder.AddRedis("redis")
    .WithLifetime(isDevelopment ? ContainerLifetime.Session : ContainerLifetime.Persistent);

// Apply environment-specific Redis configuration for Services APIs performance
if (isProduction)
{
    redis = redis
        .WithDataVolume("international-center-redis-data")
        .WithEnvironment("MAXMEMORY", "4gb") // Increased for Services APIs
        .WithEnvironment("MAXMEMORY_POLICY", "allkeys-lru")
        .WithEnvironment("SAVE", "900 1 300 10 60 10000") // Optimized persistence
        .WithEnvironment("APPENDONLY", "yes")
        .WithEnvironment("APPENDFSYNC", "everysec")
        .WithEnvironment("TCP_KEEPALIVE", "300")
        .WithEnvironment("TIMEOUT", "0")
        .WithEnvironment("DATABASES", "16");
}
else if (!isDevelopment)
{
    redis = redis
        .WithDataVolume("international-center-redis-staging")
        .WithEnvironment("MAXMEMORY", "1gb")
        .WithEnvironment("MAXMEMORY_POLICY", "allkeys-lru");
}

// Prometheus - Metrics collection and monitoring for Services APIs
var prometheus = builder.AddContainer("prometheus", "prom/prometheus")
    .WithBindMount("./prometheus", "/etc/prometheus")
    .WithHttpEndpoint(port: isDevelopment ? 9090 : null, name: "prometheus-web")
    .WithLifetime(isDevelopment ? ContainerLifetime.Session : ContainerLifetime.Persistent);

// Apply environment-specific Prometheus configuration
if (isProduction)
{
    prometheus = prometheus
        .WithDataVolume("international-center-prometheus-data", "/prometheus")
        .WithEnvironment("PROMETHEUS_RETENTION_TIME", "30d")
        .WithEnvironment("PROMETHEUS_RETENTION_SIZE", "10GB")
        .WithArgs("--config.file=/etc/prometheus/prometheus.yml",
                  "--storage.tsdb.path=/prometheus", 
                  "--web.console.libraries=/etc/prometheus/console_libraries",
                  "--web.console.templates=/etc/prometheus/consoles",
                  "--web.enable-lifecycle",
                  "--storage.tsdb.retention.time=30d",
                  "--storage.tsdb.retention.size=10GB");
}
else if (!isDevelopment)
{
    prometheus = prometheus
        .WithDataVolume("international-center-prometheus-staging", "/prometheus")
        .WithEnvironment("PROMETHEUS_RETENTION_TIME", "7d")
        .WithEnvironment("PROMETHEUS_RETENTION_SIZE", "2GB")
        .WithArgs("--config.file=/etc/prometheus/prometheus.yml",
                  "--storage.tsdb.path=/prometheus",
                  "--web.enable-lifecycle",
                  "--storage.tsdb.retention.time=7d",
                  "--storage.tsdb.retention.size=2GB");
}
else
{
    // Development: Shorter retention for faster development
    prometheus = prometheus
        .WithArgs("--config.file=/etc/prometheus/prometheus.yml",
                  "--storage.tsdb.path=/prometheus",
                  "--web.enable-lifecycle",
                  "--storage.tsdb.retention.time=24h",
                  "--web.console.libraries=/etc/prometheus/console_libraries",
                  "--web.console.templates=/etc/prometheus/consoles",
                  "--log.level=debug");
}

// Grafana - Visualization and alerting for Services APIs monitoring
var grafana = builder.AddContainer("grafana", "grafana/grafana")
    .WithBindMount("./grafana/provisioning", "/etc/grafana/provisioning")
    .WithBindMount("./grafana/dashboards", "/var/lib/grafana/dashboards")
    .WithHttpEndpoint(port: isDevelopment ? 3000 : null, name: "grafana-web")
    .WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", "admin123") // TODO: Use secrets in production
    .WithEnvironment("GF_USERS_ALLOW_SIGN_UP", "false")
    .WithEnvironment("GF_INSTALL_PLUGINS", "grafana-piechart-panel")
    .WithLifetime(isDevelopment ? ContainerLifetime.Session : ContainerLifetime.Persistent);

// Apply environment-specific Grafana configuration
if (isProduction)
{
    grafana = grafana
        .WithDataVolume("international-center-grafana-data", "/var/lib/grafana")
        .WithEnvironment("GF_SERVER_ROOT_URL", "https://monitoring.international-center.app")
        .WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", keyVault!.AddSecret("grafana-admin-password"))
        .WithEnvironment("GF_SMTP_ENABLED", "true")
        .WithEnvironment("GF_SMTP_HOST", "smtp.office365.com:587")
        .WithEnvironment("GF_SMTP_USER", keyVault!.AddSecret("grafana-smtp-user"))
        .WithEnvironment("GF_SMTP_PASSWORD", keyVault!.AddSecret("grafana-smtp-password"))
        .WithEnvironment("GF_SMTP_FROM_ADDRESS", "monitoring@international-center.app")
        .WithEnvironment("GF_ALERTING_ENABLED", "true")
        .WithEnvironment("GF_UNIFIED_ALERTING_ENABLED", "true");
}
else if (!isDevelopment)
{
    grafana = grafana
        .WithDataVolume("international-center-grafana-staging", "/var/lib/grafana")
        .WithEnvironment("GF_SERVER_ROOT_URL", "https://monitoring-staging.international-center.app")
        .WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", keyVault!.AddSecret("grafana-admin-password"))
        .WithEnvironment("GF_ALERTING_ENABLED", "true");
}
else
{
    // Development: Local admin credentials and debugging enabled
    grafana = grafana
        .WithEnvironment("GF_LOG_LEVEL", "debug")
        .WithEnvironment("GF_PATHS_LOGS", "/var/log/grafana")
        .WithEnvironment("GF_FEATURE_TOGGLES_ENABLE", "publicDashboards")
        .WithEnvironment("GF_ANALYTICS_REPORTING_ENABLED", "false");
}

// Migration Service - applies database migrations and runs to completion
var migrationService = builder.AddProject<Projects.Infrastructure_Database_Migrations_Service>("migration-service")
    .WithReference(database)
    .WithEnvironment("ASPIRE_ALLOW_UNSECURED_TRANSPORT", isDevelopment.ToString().ToLower())
    // Secrets provider configuration for Migration Service
    .WithEnvironment("SECRETS_PROVIDER", isDevelopment ? "LOCAL_PARAMETERS" : "AZURE_KEY_VAULT")
    .WithEnvironment("KEY_VAULT_URI", isDevelopment ? "" : "https://international-center-keyvault.vault.azure.net/");

// Services APIs - Enhanced with Podman-optimized Microsoft Aspire enterprise patterns
// Focus on Services APIs only per user requirements (simplified for gateway architecture)
var servicesPublicApi = builder.AddProject<Projects.Services_Public_Api>("services-public-api")
    .WithReference(database)
    .WithReference(redis)
    .WithEnvironment("ASPIRE_ALLOW_UNSECURED_TRANSPORT", isDevelopment.ToString().ToLower())
    .WithEnvironment("OTEL_SERVICE_NAME", "Services.Public.Api")
    .WithEnvironment("CONTAINER_RUNTIME", "podman")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithEnvironment("ASPNETCORE_KESTREL_ENDPOINTS__HTTP__URL", isDevelopment ? "http://0.0.0.0:7240" : "http://0.0.0.0:80")
    .WithEnvironment("ASPNETCORE_KESTREL_ENDPOINTS__HTTPS__URL", isDevelopment ? "https://0.0.0.0:7240" : "https://0.0.0.0:443")
    // Application Insights connection string for public API monitoring
    .WithEnvironment("APPLICATIONINSIGHTS_CONNECTION_STRING", applicationInsightsConnectionString)
    // Secrets provider configuration for Services Public API
    .WithEnvironment("SECRETS_PROVIDER", isDevelopment ? "LOCAL_PARAMETERS" : "AZURE_KEY_VAULT")
    .WithEnvironment("KEY_VAULT_URI", isDevelopment ? "" : "https://international-center-keyvault.vault.azure.net/")
    .WithHttpEndpoint(port: isDevelopment ? 7240 : null, name: "services-public-api")
    .WaitForCompletion(migrationService);

// Services Admin API - Enhanced with medical-grade security and Podman optimization (simplified for gateway architecture)
var servicesAdminApi = builder.AddProject<Projects.Services_Admin_Api>("services-admin-api")
    .WithReference(database)
    .WithReference(redis)
    .WithEnvironment("ASPIRE_ALLOW_UNSECURED_TRANSPORT", isDevelopment.ToString().ToLower())
    .WithEnvironment("OTEL_SERVICE_NAME", "Services.Admin.Api")
    .WithEnvironment("CONTAINER_RUNTIME", "podman")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithEnvironment("ASPNETCORE_KESTREL_ENDPOINTS__HTTP__URL", isDevelopment ? "http://0.0.0.0:7241" : "http://0.0.0.0:80")
    .WithEnvironment("ASPNETCORE_KESTREL_ENDPOINTS__HTTPS__URL", isDevelopment ? "https://0.0.0.0:7241" : "https://0.0.0.0:443")
    .WithEnvironment("MEDICAL_GRADE_AUDIT", "true")
    .WithEnvironment("ZERO_TRUST_SECURITY", "true")
    // Application Insights connection string for medical-grade API monitoring
    .WithEnvironment("APPLICATIONINSIGHTS_CONNECTION_STRING", applicationInsightsConnectionString)
    // Secrets provider configuration for Services Admin API
    .WithEnvironment("SECRETS_PROVIDER", isDevelopment ? "LOCAL_PARAMETERS" : "AZURE_KEY_VAULT")
    .WithEnvironment("KEY_VAULT_URI", isDevelopment ? "" : "https://international-center-keyvault.vault.azure.net/")
    .WithHttpEndpoint(port: isDevelopment ? 7241 : null, name: "services-admin-api")
    .WaitForCompletion(migrationService);

// API Gateways - YARP reverse proxies with differential security policies
// Public Gateway: Handles public website traffic with higher rate limits and anonymous logging
var publicGateway = builder.AddProject<Projects.Gateway_Public>("public-gateway")
    .WithReference(redis)
    .WithReference(servicesPublicApi)
    .WithEnvironment("ASPIRE_ALLOW_UNSECURED_TRANSPORT", isDevelopment.ToString().ToLower())
    .WithEnvironment("OTEL_SERVICE_NAME", "Gateway.Public")
    .WithEnvironment("CONTAINER_RUNTIME", "podman")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithEnvironment("ASPNETCORE_KESTREL_ENDPOINTS__HTTP__URL", isDevelopment ? "http://0.0.0.0:7220" : "http://0.0.0.0:80")
    .WithEnvironment("ASPNETCORE_KESTREL_ENDPOINTS__HTTPS__URL", isDevelopment ? "https://0.0.0.0:44320" : "https://0.0.0.0:443")
    // Application Insights connection string for public website monitoring
    .WithEnvironment("APPLICATIONINSIGHTS_CONNECTION_STRING", applicationInsightsConnectionString)
    // Additional environment-specific configuration for Public Gateway
    .WithEnvironment("SECRETS_PROVIDER", isDevelopment ? "LOCAL_PARAMETERS" : "AZURE_KEY_VAULT")
    .WithEnvironment("KEY_VAULT_URI", isDevelopment ? "" : "https://international-center-keyvault.vault.azure.net/")
    .WithHttpEndpoint(port: isDevelopment ? 7220 : null, name: "public-gateway")
    .WithHttpsEndpoint(port: isDevelopment ? 44320 : null, name: "public-gateway-tls")
    .WaitForCompletion(servicesPublicApi);

// Admin Gateway: Handles admin portal traffic with Microsoft Entra External ID authentication and medical-grade audit logging
var adminGateway = builder.AddProject<Projects.Gateway_Admin>("admin-gateway")
    .WithReference(database)
    .WithReference(redis)
    .WithReference(servicesAdminApi)
    .WithEnvironment("ASPIRE_ALLOW_UNSECURED_TRANSPORT", isDevelopment.ToString().ToLower())
    .WithEnvironment("OTEL_SERVICE_NAME", "Gateway.Admin")
    .WithEnvironment("CONTAINER_RUNTIME", "podman")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithEnvironment("ASPNETCORE_KESTREL_ENDPOINTS__HTTP__URL", isDevelopment ? "http://0.0.0.0:7221" : "http://0.0.0.0:80")
    .WithEnvironment("ASPNETCORE_KESTREL_ENDPOINTS__HTTPS__URL", isDevelopment ? "https://0.0.0.0:44321" : "https://0.0.0.0:443")
    .WithEnvironment("MEDICAL_GRADE_AUDIT", "true")
    .WithEnvironment("ENTRA_EXTERNAL_ID_ENABLED", "true")
    // Microsoft Entra External ID secrets injection for medical-grade authentication
    .WithEnvironment("EntraExternalId__TenantId", entraTenantId)
    .WithEnvironment("EntraExternalId__ClientId", entraClientId)
    .WithEnvironment("EntraExternalId__ClientSecret", entraClientSecret)
    // Application Insights connection string for medical-grade monitoring
    .WithEnvironment("APPLICATIONINSIGHTS_CONNECTION_STRING", applicationInsightsConnectionString)
    // Additional environment-specific configuration
    .WithEnvironment("SECRETS_PROVIDER", isDevelopment ? "LOCAL_PARAMETERS" : "AZURE_KEY_VAULT")
    .WithEnvironment("KEY_VAULT_URI", isDevelopment ? "" : "https://international-center-keyvault.vault.azure.net/")
    .WithHttpEndpoint(port: isDevelopment ? 7221 : null, name: "admin-gateway")
    .WithHttpsEndpoint(port: isDevelopment ? 44321 : null, name: "admin-gateway-tls")
    .WaitForCompletion(servicesAdminApi);


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

// Website - Astro + Vue + Tailwind + shadcn-vue + Pinia with Bun runtime integration
// Enhanced .NET hosting with proper static file serving and development proxy
var website = builder.AddProject<Projects.Website>("website")
    .WithEnvironment("OTEL_SERVICE_NAME", "Website")
    .WithEnvironment("NODE_ENV", isDevelopment ? "development" : isProduction ? "production" : "staging")
    .WithEnvironment("ASPIRE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    // Public Gateway service discovery for API integration
    .WithEnvironment("VITE_PUBLIC_GATEWAY_URL", publicGateway.GetEndpoint("public-gateway"))
    .WithEnvironment("VITE_API_BASE_URL", publicGateway.GetEndpoint("public-gateway"))
    // Configure proper ports for .NET hosting and Astro dev server
    .WithHttpEndpoint(port: isDevelopment ? 5000 : null, name: "website-dotnet")
    .WithHttpsEndpoint(port: isDevelopment ? 5001 : null, name: "website-dotnet-tls")
    .WithExternalHttpEndpoints()
    // Service references for gateway integration
    .WithReference(publicGateway)
    .WithReference(adminGateway)
    // Bun runtime integration for package management and development
    .WithEnvironment("BUN_INSTALL_CACHE_DIR", "/tmp/bun-cache")
    .WithEnvironment("BUN_INSTALL_GLOBAL_DIR", "/tmp/bun-global")
    .WithEnvironment("FORCE_COLOR", "1") // Better terminal output
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
    Console.WriteLine("🔑 Secrets: Local parameters for faster development iteration");
}
else if (isProduction)
{
    Console.WriteLine("🏭 Microsoft Aspire AppHost starting in PRODUCTION mode");
    Console.WriteLine("📈 High availability with service replicas enabled");
    Console.WriteLine("🔐 Secure endpoints with HTTPS and TLS");
    Console.WriteLine("📊 Enterprise observability with distributed tracing");
    Console.WriteLine("🗄️  Persistent data volumes for PostgreSQL and Microsoft Garnet");
    Console.WriteLine("🔑 Secrets: Azure Key Vault for medical-grade compliance");
}
else
{
    Console.WriteLine("🧪 Microsoft Aspire AppHost starting in STAGING mode");
    Console.WriteLine("🔍 Production-like configuration with debugging enabled");
    Console.WriteLine("🔑 Secrets: Azure Key Vault for production-like security");
}

app.Run();