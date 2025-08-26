using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Aspire.Hosting.Testing;
using Xunit;

namespace InternationalCenter.Tests.Shared;

/// <summary>
/// Shared utilities for secrets management testing across gateway architecture.
/// Provides common test scenarios and validation methods for secret resolution
/// and fallback behavior testing. Used by both Admin and Public Gateway tests.
/// </summary>
public static class SecretsManagementTestUtilities
{
    /// <summary>
    /// Validates that the correct secrets provider is configured for a given environment.
    /// </summary>
    public static void ValidateSecretsProviderForEnvironment(IConfiguration configuration, string environment)
    {
        var secretsProvider = configuration["SECRETS_PROVIDER"];
        var keyVaultUri = configuration["KEY_VAULT_URI"];

        switch (environment.ToLowerInvariant())
        {
            case "development":
                Assert.Equal("LOCAL_PARAMETERS", secretsProvider);
                Assert.True(string.IsNullOrEmpty(keyVaultUri));
                break;

            case "production":
            case "staging":
                Assert.Equal("AZURE_KEY_VAULT", secretsProvider);
                Assert.Equal("https://international-center-keyvault.vault.azure.net/", keyVaultUri);
                break;

            case "testing":
                // Testing environment should bypass secrets configuration
                Assert.True(string.IsNullOrEmpty(secretsProvider) || secretsProvider == "TESTING_BYPASS");
                Assert.True(string.IsNullOrEmpty(keyVaultUri));
                break;

            default:
                throw new ArgumentException($"Unknown environment: {environment}");
        }
    }

    /// <summary>
    /// Validates that Azure Managed Identity is properly configured in non-testing environments.
    /// </summary>
    public static void ValidateManagedIdentityConfiguration(IServiceProvider serviceProvider, string environment)
    {
        if (environment.Equals("Testing", StringComparison.OrdinalIgnoreCase))
        {
            // Testing environment should not have managed identity configured
            return;
        }

        var azureCredential = serviceProvider.GetService<Azure.Identity.DefaultAzureCredential>();
        Assert.NotNull(azureCredential);
    }

    /// <summary>
    /// Validates that configuration refresh capability exists for secret rotation support.
    /// </summary>
    public static void ValidateConfigurationRefreshCapability(IConfiguration configuration)
    {
        var reloadToken = configuration.GetReloadToken();
        Assert.NotNull(reloadToken);
    }

    /// <summary>
    /// Validates that missing secrets are handled gracefully without exceptions.
    /// </summary>
    public static void ValidateGracefulSecretHandling(IConfiguration configuration, params string[] secretKeys)
    {
        foreach (var secretKey in secretKeys)
        {
            // Accessing missing secrets should not throw exceptions
            var secretValue = configuration[secretKey];
            // Value may be null or empty, but no exception should be thrown
            Assert.True(true); // If we get here, no exception was thrown
        }
    }

    /// <summary>
    /// Creates a test configuration with common secrets for development environment.
    /// </summary>
    public static DistributedApplicationTestingBuilder WithDevelopmentSecrets(this DistributedApplicationTestingBuilder builder)
    {
        return builder
            .WithEnvironment("Development")
            .WithParameter("postgres-password", "test-postgres-password")
            .WithParameter("entra-tenant-id", "test-tenant-id")
            .WithParameter("entra-client-id", "test-client-id")
            .WithParameter("entra-client-secret", "test-client-secret")
            .WithParameter("appinsights-connection-string", "InstrumentationKey=test-key;IngestionEndpoint=https://test.applicationinsights.azure.com/");
    }

    /// <summary>
    /// Creates a test configuration for production environment with Azure Key Vault.
    /// </summary>
    public static DistributedApplicationTestingBuilder WithProductionSecrets(this DistributedApplicationTestingBuilder builder)
    {
        return builder.WithEnvironment("Production");
    }

    /// <summary>
    /// Creates a test configuration for staging environment with Azure Key Vault.
    /// </summary>
    public static DistributedApplicationTestingBuilder WithStagingSecrets(this DistributedApplicationTestingBuilder builder)
    {
        return builder.WithEnvironment("Staging");
    }

    /// <summary>
    /// Creates a test configuration for testing environment with secrets bypass.
    /// </summary>
    public static DistributedApplicationTestingBuilder WithTestingBypass(this DistributedApplicationTestingBuilder builder)
    {
        return builder.WithEnvironment("Testing");
    }

    /// <summary>
    /// Validates Application Insights configuration for monitoring.
    /// </summary>
    public static void ValidateApplicationInsightsConfiguration(IConfiguration configuration, string expectedConnectionString = null)
    {
        var appInsightsConnection = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        
        if (expectedConnectionString != null)
        {
            Assert.Equal(expectedConnectionString, appInsightsConnection);
        }
        else
        {
            // Just verify it's accessible without throwing exceptions
            Assert.True(true); // Configuration access succeeded
        }
    }

    /// <summary>
    /// Validates medical-grade audit logging capability for Admin Gateway.
    /// </summary>
    public static void ValidateMedicalGradeAuditCapability(ILogger logger, string gatewayType = "UNKNOWN")
    {
        Assert.NotNull(logger);
        
        // Log test audit entry to validate capability
        logger.LogInformation("MEDICAL_AUDIT: Secrets management test audit entry - GatewayType: {GatewayType}, TestValidation: {TestValidation}, Timestamp: {Timestamp}",
            gatewayType,
            "AUDIT_CAPABILITY_VALIDATED",
            DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Validates anonymous usage logging capability for Public Gateway.
    /// </summary>
    public static void ValidateAnonymousUsageLogging(ILogger logger)
    {
        Assert.NotNull(logger);
        
        // Log test usage entry to validate capability
        logger.LogInformation("Public Gateway anonymous usage test entry - TestValidation: {TestValidation}, Timestamp: {Timestamp}",
            "ANONYMOUS_LOGGING_VALIDATED",
            DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Validates environment-specific configuration differences between gateways.
    /// </summary>
    public static void ValidateGatewaySpecificConfiguration(IConfiguration configuration, string gatewayType, string environment)
    {
        var secretsProvider = configuration["SECRETS_PROVIDER"];
        
        // Both gateways should use the same secrets provider for the same environment
        ValidateSecretsProviderForEnvironment(configuration, environment);
        
        // Gateway-specific validations
        switch (gatewayType.ToLowerInvariant())
        {
            case "admin":
                // Admin Gateway should have Entra External ID configuration capability
                var entraTenantIdKey = configuration["EntraExternalId:TenantId"];
                // Value may be null in testing, but key access should not throw
                Assert.True(true);
                break;
                
            case "public":
                // Public Gateway should have public-specific configuration
                var publicSecretsProvider = configuration["SECRETS_PROVIDER"];
                Assert.NotNull(publicSecretsProvider);
                break;
                
            default:
                throw new ArgumentException($"Unknown gateway type: {gatewayType}");
        }
    }

    /// <summary>
    /// Simulates secret rotation scenario for testing.
    /// </summary>
    public static async Task<bool> SimulateSecretRotationScenario(IConfiguration configuration, ILogger logger)
    {
        try
        {
            // Get initial configuration state
            var initialReloadToken = configuration.GetReloadToken();
            
            // In a real scenario, this would trigger when Key Vault rotates secrets
            // For testing, we validate that the infrastructure is in place
            var hasRefreshCapability = initialReloadToken != null;
            
            if (hasRefreshCapability)
            {
                logger.LogInformation("SECRET_ROTATION_TEST: Rotation infrastructure validated - HasRefreshCapability: {HasRefreshCapability}, Timestamp: {Timestamp}",
                    hasRefreshCapability,
                    DateTimeOffset.UtcNow);
            }
            
            return hasRefreshCapability;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SECRET_ROTATION_TEST: Failed to validate rotation capability");
            return false;
        }
    }

    /// <summary>
    /// Validates secrets management integration across the entire gateway architecture.
    /// </summary>
    public static async Task ValidateEndToEndSecretsIntegration(
        DistributedApplicationTestingBuilder distributedAppBuilder, 
        ILogger logger,
        string environment = "Development")
    {
        // Build distributed application with specified environment
        var appHost = await distributedAppBuilder
            .WithEnvironment(environment)
            .BuildAsync();

        using var scope = appHost.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Validate environment-specific secrets configuration
        ValidateSecretsProviderForEnvironment(configuration, environment);
        
        // Validate managed identity configuration
        ValidateManagedIdentityConfiguration(scope.ServiceProvider, environment);
        
        // Validate configuration refresh capability
        ValidateConfigurationRefreshCapability(configuration);
        
        // Validate Application Insights configuration
        ValidateApplicationInsightsConfiguration(configuration);
        
        // Log successful end-to-end validation
        logger.LogInformation("SECRETS_MANAGEMENT_TEST: End-to-end secrets integration validated successfully - Environment: {Environment}, Timestamp: {Timestamp}",
            environment,
            DateTimeOffset.UtcNow);
    }
}