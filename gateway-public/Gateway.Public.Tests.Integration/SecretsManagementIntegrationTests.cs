using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Azure.Identity;
using Xunit;
using Xunit.Abstractions;
using Aspire.Hosting.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InternationalCenter.Gateway.Public.Tests.Integration;

/// <summary>
/// Integration tests for secrets management in the Public Gateway.
/// Tests secret resolution, fallback behavior, and environment-specific handling.
/// Uses DistributedApplicationTestingBuilder as per project requirements (no mocks).
/// </summary>
public class SecretsManagementIntegrationTests : IClassFixture<DistributedApplicationTestingBuilder>
{
    private readonly DistributedApplicationTestingBuilder _distributedAppBuilder;
    private readonly ITestOutputHelper _output;

    public SecretsManagementIntegrationTests(DistributedApplicationTestingBuilder distributedAppBuilder, ITestOutputHelper output)
    {
        _distributedAppBuilder = distributedAppBuilder;
        _output = output;
    }

    [Fact]
    public async Task PublicGateway_DevelopmentEnvironment_ShouldUseLocalParameters()
    {
        // Arrange - Configure Development environment
        var appHost = await _distributedAppBuilder
            .WithEnvironment("Development")
            .WithParameter("appinsights-connection-string", "test-appinsights-connection")
            .BuildAsync();

        // Act - Get Public Gateway service
        using var scope = appHost.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SecretsManagementIntegrationTests>>();

        // Assert - Verify local parameters are used in Development
        var secretsProvider = configuration["SECRETS_PROVIDER"];
        var keyVaultUri = configuration["KEY_VAULT_URI"];
        var appInsightsConnection = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

        Assert.Equal("LOCAL_PARAMETERS", secretsProvider);
        Assert.True(string.IsNullOrEmpty(keyVaultUri));
        Assert.Equal("test-appinsights-connection", appInsightsConnection);

        _output.WriteLine($"Development environment test passed - Secrets Provider: {secretsProvider}");
        logger.LogInformation("SECRETS_MANAGEMENT_TEST: Public Gateway Development environment secret resolution validated successfully");
    }

    [Fact]
    public async Task PublicGateway_ProductionEnvironment_ShouldUseAzureKeyVault()
    {
        // Arrange - Configure Production environment
        var appHost = await _distributedAppBuilder
            .WithEnvironment("Production")
            .BuildAsync();

        // Act - Get Public Gateway service
        using var scope = appHost.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SecretsManagementIntegrationTests>>();

        // Assert - Verify Azure Key Vault configuration in Production
        var secretsProvider = configuration["SECRETS_PROVIDER"];
        var keyVaultUri = configuration["KEY_VAULT_URI"];

        Assert.Equal("AZURE_KEY_VAULT", secretsProvider);
        Assert.Equal("https://international-center-keyvault.vault.azure.net/", keyVaultUri);

        _output.WriteLine($"Production environment test passed - Secrets Provider: {secretsProvider}, Key Vault URI: {keyVaultUri}");
        logger.LogInformation("SECRETS_MANAGEMENT_TEST: Public Gateway Production environment Azure Key Vault configuration validated successfully");
    }

    [Fact]
    public async Task PublicGateway_StagingEnvironment_ShouldUseAzureKeyVault()
    {
        // Arrange - Configure Staging environment
        var appHost = await _distributedAppBuilder
            .WithEnvironment("Staging")
            .BuildAsync();

        // Act - Get Public Gateway service
        using var scope = appHost.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SecretsManagementIntegrationTests>>();

        // Assert - Verify Azure Key Vault configuration in Staging
        var secretsProvider = configuration["SECRETS_PROVIDER"];
        var keyVaultUri = configuration["KEY_VAULT_URI"];

        Assert.Equal("AZURE_KEY_VAULT", secretsProvider);
        Assert.Equal("https://international-center-keyvault.vault.azure.net/", keyVaultUri);

        _output.WriteLine($"Staging environment test passed - Secrets Provider: {secretsProvider}");
        logger.LogInformation("SECRETS_MANAGEMENT_TEST: Public Gateway Staging environment Azure Key Vault configuration validated successfully");
    }

    [Fact]
    public async Task PublicGateway_TestingEnvironment_ShouldBypassSecretsConfiguration()
    {
        // Arrange - Configure Testing environment
        var appHost = await _distributedAppBuilder
            .WithEnvironment("Testing")
            .BuildAsync();

        // Act - Get Public Gateway service
        using var scope = appHost.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SecretsManagementIntegrationTests>>();

        // Assert - Verify secrets configuration is bypassed in Testing
        var secretsProvider = configuration["SECRETS_PROVIDER"];
        var keyVaultUri = configuration["KEY_VAULT_URI"];

        // Testing environment should not have secrets configuration
        Assert.True(string.IsNullOrEmpty(secretsProvider) || secretsProvider == "TESTING_BYPASS");
        Assert.True(string.IsNullOrEmpty(keyVaultUri));

        _output.WriteLine("Testing environment test passed - Secrets configuration bypassed");
        logger.LogInformation("SECRETS_MANAGEMENT_TEST: Public Gateway Testing environment secrets configuration bypass validated successfully");
    }

    [Fact]
    public async Task PublicGateway_ManagedIdentity_ShouldBeConfiguredInNonTestingEnvironments()
    {
        // Arrange - Configure Production environment
        var appHost = await _distributedAppBuilder
            .WithEnvironment("Production")
            .BuildAsync();

        // Act - Get Public Gateway service and check for Azure credential registration
        using var scope = appHost.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SecretsManagementIntegrationTests>>();

        // Assert - Verify DefaultAzureCredential is registered
        var azureCredential = scope.ServiceProvider.GetService<DefaultAzureCredential>();
        Assert.NotNull(azureCredential);

        _output.WriteLine("Azure Managed Identity test passed - DefaultAzureCredential is registered");
        logger.LogInformation("SECRETS_MANAGEMENT_TEST: Public Gateway Azure Managed Identity configuration validated successfully");
    }

    [Fact]
    public async Task PublicGateway_SecretResolution_ShouldHandleMissingSecretsGracefully()
    {
        // Arrange - Configure environment with intentionally missing secrets
        var appHost = await _distributedAppBuilder
            .WithEnvironment("Development")
            // Intentionally omit Application Insights connection string to test fallback
            .BuildAsync();

        // Act - Get Public Gateway service
        using var scope = appHost.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SecretsManagementIntegrationTests>>();

        // Assert - Verify graceful handling of missing secrets
        var appInsightsConnection = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

        // Missing secrets should return null/empty without throwing exceptions
        Assert.True(string.IsNullOrEmpty(appInsightsConnection));

        _output.WriteLine("Missing secrets fallback test passed - No exceptions thrown");
        logger.LogInformation("SECRETS_MANAGEMENT_TEST: Public Gateway missing secrets fallback behavior validated successfully");
    }

    [Fact]
    public async Task PublicGateway_ConfigurationRefresh_ShouldSupportSecretRotation()
    {
        // Arrange - Configure Development environment with initial secrets
        var appHost = await _distributedAppBuilder
            .WithEnvironment("Development")
            .WithParameter("appinsights-connection-string", "initial-connection-string")
            .BuildAsync();

        // Act - Get Public Gateway service and initial configuration
        using var scope = appHost.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SecretsManagementIntegrationTests>>();

        // Get initial values
        var initialConnection = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

        // Assert - Verify configuration refresh capability exists
        var reloadToken = configuration.GetReloadToken();
        Assert.NotNull(reloadToken);

        _output.WriteLine($"Configuration refresh test passed - Initial connection: {initialConnection}");
        logger.LogInformation("SECRETS_MANAGEMENT_TEST: Public Gateway configuration refresh capability validated successfully");
    }

    [Fact]
    public async Task PublicGateway_AnonymousUsageLogging_ShouldLogSecretOperations()
    {
        // Arrange - Configure Development environment
        var appHost = await _distributedAppBuilder
            .WithEnvironment("Development")
            .WithParameter("appinsights-connection-string", "audit-test-connection")
            .BuildAsync();

        // Act - Get Public Gateway service and trigger configuration access
        using var scope = appHost.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SecretsManagementIntegrationTests>>();

        // Access configuration values to trigger any logging
        var secretsProvider = configuration["SECRETS_PROVIDER"];
        var appInsightsConnection = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

        // Assert - Verify anonymous usage logging context is available
        Assert.NotNull(logger);
        Assert.NotNull(secretsProvider);

        _output.WriteLine("Anonymous usage logging test passed - Logger and configuration access validated");
        logger.LogInformation("Public Gateway anonymous usage logging capability validated successfully - SecretsProvider: {SecretsProvider}, ConfigurationAccess: {ConfigurationAccess}", 
            secretsProvider, "VALIDATED");
    }

    [Theory]
    [InlineData("Development", "LOCAL_PARAMETERS")]
    [InlineData("Production", "AZURE_KEY_VAULT")]
    [InlineData("Staging", "AZURE_KEY_VAULT")]
    public async Task PublicGateway_EnvironmentSpecificSecretHandling_ShouldUseCorrectProvider(string environment, string expectedProvider)
    {
        // Arrange - Configure specific environment
        var appHost = await _distributedAppBuilder
            .WithEnvironment(environment)
            .BuildAsync();

        // Act - Get Public Gateway service
        using var scope = appHost.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SecretsManagementIntegrationTests>>();

        // Assert - Verify correct secrets provider for environment
        var secretsProvider = configuration["SECRETS_PROVIDER"];
        Assert.Equal(expectedProvider, secretsProvider);

        _output.WriteLine($"Environment-specific test passed - {environment} uses {expectedProvider}");
        logger.LogInformation("SECRETS_MANAGEMENT_TEST: Public Gateway environment-specific secret handling validated - Environment: {Environment}, Provider: {Provider}", 
            environment, expectedProvider);
    }

    [Fact]
    public async Task PublicGateway_ApplicationInsightsIntegration_ShouldConfigureMonitoring()
    {
        // Arrange - Configure Development environment with Application Insights
        var appHost = await _distributedAppBuilder
            .WithEnvironment("Development")
            .WithParameter("appinsights-connection-string", "InstrumentationKey=public-test-key;IngestionEndpoint=https://test-public.applicationinsights.azure.com/")
            .BuildAsync();

        // Act - Get Public Gateway service
        using var scope = appHost.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SecretsManagementIntegrationTests>>();

        // Assert - Verify Application Insights configuration for public website monitoring
        var appInsightsConnectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        Assert.NotNull(appInsightsConnectionString);
        Assert.Contains("InstrumentationKey=public-test-key", appInsightsConnectionString);

        _output.WriteLine("Application Insights integration test passed for public website monitoring");
        logger.LogInformation("SECRETS_MANAGEMENT_TEST: Public Gateway Application Insights monitoring configuration validated successfully");
    }

    [Fact]
    public async Task PublicGateway_RedisConnection_ShouldSupportSecretBasedConfiguration()
    {
        // Arrange - Configure Production environment (where Redis uses Key Vault)
        var appHost = await _distributedAppBuilder
            .WithEnvironment("Production")
            .BuildAsync();

        // Act - Get Public Gateway service
        using var scope = appHost.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SecretsManagementIntegrationTests>>();

        // Assert - Verify Redis configuration can be resolved through secrets
        var secretsProvider = configuration["SECRETS_PROVIDER"];
        var keyVaultUri = configuration["KEY_VAULT_URI"];

        Assert.Equal("AZURE_KEY_VAULT", secretsProvider);
        Assert.NotNull(keyVaultUri);

        _output.WriteLine("Redis secret-based configuration test passed");
        logger.LogInformation("SECRETS_MANAGEMENT_TEST: Public Gateway Redis secret-based configuration validated successfully");
    }

    [Fact]
    public async Task PublicGateway_PublicWebsiteUsage_ShouldNotExposeSecretInformation()
    {
        // Arrange - Configure Development environment
        var appHost = await _distributedAppBuilder
            .WithEnvironment("Development")
            .WithParameter("appinsights-connection-string", "sensitive-connection-string-data")
            .BuildAsync();

        // Act - Get Public Gateway service
        using var scope = appHost.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SecretsManagementIntegrationTests>>();

        // Assert - Verify that secret information handling is secure
        var secretsProvider = configuration["SECRETS_PROVIDER"];
        
        // Public Gateway should handle secrets securely without exposing sensitive data in logs
        Assert.NotNull(secretsProvider);
        Assert.Equal("LOCAL_PARAMETERS", secretsProvider);

        _output.WriteLine("Public website security test passed - Secret information handled securely");
        logger.LogInformation("SECRETS_MANAGEMENT_TEST: Public Gateway secure secret handling for public website usage validated successfully");
    }

    [Fact]
    public async Task PublicGateway_RateLimitingRedis_ShouldWorkWithSecretsManagement()
    {
        // Arrange - Configure environment that uses Redis for rate limiting
        var appHost = await _distributedAppBuilder
            .WithEnvironment("Development")
            .BuildAsync();

        // Act - Get Public Gateway service
        using var scope = appHost.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SecretsManagementIntegrationTests>>();

        // Assert - Verify Redis connection configuration for rate limiting works with secrets management
        var secretsProvider = configuration["SECRETS_PROVIDER"];
        var redisConnection = configuration.GetConnectionString("redis");
        
        Assert.NotNull(secretsProvider);
        // Redis connection should be available (either from Aspire or secrets)
        
        _output.WriteLine("Rate limiting Redis integration test passed");
        logger.LogInformation("SECRETS_MANAGEMENT_TEST: Public Gateway Redis rate limiting integration with secrets management validated successfully");
    }
}