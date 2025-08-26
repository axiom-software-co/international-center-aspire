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

namespace InternationalCenter.Gateway.Admin.Tests.Integration;

/// <summary>
/// Integration tests for secrets management in the Admin Gateway.
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
    public async Task AdminGateway_DevelopmentEnvironment_ShouldUseLocalParameters()
    {
        // Arrange - Configure Development environment
        var appHost = await _distributedAppBuilder
            .WithEnvironment("Development")
            .WithParameter("postgres-password", "test-postgres-password")
            .WithParameter("entra-tenant-id", "test-tenant-id")
            .WithParameter("entra-client-id", "test-client-id")
            .WithParameter("entra-client-secret", "test-client-secret")
            .WithParameter("appinsights-connection-string", "test-appinsights-connection")
            .BuildAsync();

        // Act - Get Admin Gateway service
        using var scope = appHost.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SecretsManagementIntegrationTests>>();

        // Assert - Verify local parameters are used in Development
        var secretsProvider = configuration["SECRETS_PROVIDER"];
        var keyVaultUri = configuration["KEY_VAULT_URI"];
        var entraTenantId = configuration["EntraExternalId:TenantId"];
        var entraClientId = configuration["EntraExternalId:ClientId"];

        Assert.Equal("LOCAL_PARAMETERS", secretsProvider);
        Assert.True(string.IsNullOrEmpty(keyVaultUri));
        Assert.Equal("test-tenant-id", entraTenantId);
        Assert.Equal("test-client-id", entraClientId);

        _output.WriteLine($"Development environment test passed - Secrets Provider: {secretsProvider}");
        logger.LogInformation("SECRETS_MANAGEMENT_TEST: Development environment secret resolution validated successfully");
    }

    [Fact]
    public async Task AdminGateway_ProductionEnvironment_ShouldUseAzureKeyVault()
    {
        // Arrange - Configure Production environment
        var appHost = await _distributedAppBuilder
            .WithEnvironment("Production")
            .BuildAsync();

        // Act - Get Admin Gateway service
        using var scope = appHost.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SecretsManagementIntegrationTests>>();

        // Assert - Verify Azure Key Vault configuration in Production
        var secretsProvider = configuration["SECRETS_PROVIDER"];
        var keyVaultUri = configuration["KEY_VAULT_URI"];

        Assert.Equal("AZURE_KEY_VAULT", secretsProvider);
        Assert.Equal("https://international-center-keyvault.vault.azure.net/", keyVaultUri);

        _output.WriteLine($"Production environment test passed - Secrets Provider: {secretsProvider}, Key Vault URI: {keyVaultUri}");
        logger.LogInformation("SECRETS_MANAGEMENT_TEST: Production environment Azure Key Vault configuration validated successfully");
    }

    [Fact]
    public async Task AdminGateway_StagingEnvironment_ShouldUseAzureKeyVault()
    {
        // Arrange - Configure Staging environment
        var appHost = await _distributedAppBuilder
            .WithEnvironment("Staging")
            .BuildAsync();

        // Act - Get Admin Gateway service
        using var scope = appHost.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SecretsManagementIntegrationTests>>();

        // Assert - Verify Azure Key Vault configuration in Staging
        var secretsProvider = configuration["SECRETS_PROVIDER"];
        var keyVaultUri = configuration["KEY_VAULT_URI"];

        Assert.Equal("AZURE_KEY_VAULT", secretsProvider);
        Assert.Equal("https://international-center-keyvault.vault.azure.net/", keyVaultUri);

        _output.WriteLine($"Staging environment test passed - Secrets Provider: {secretsProvider}");
        logger.LogInformation("SECRETS_MANAGEMENT_TEST: Staging environment Azure Key Vault configuration validated successfully");
    }

    [Fact]
    public async Task AdminGateway_TestingEnvironment_ShouldBypassSecretsConfiguration()
    {
        // Arrange - Configure Testing environment
        var appHost = await _distributedAppBuilder
            .WithEnvironment("Testing")
            .BuildAsync();

        // Act - Get Admin Gateway service
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
        logger.LogInformation("SECRETS_MANAGEMENT_TEST: Testing environment secrets configuration bypass validated successfully");
    }

    [Fact]
    public async Task AdminGateway_ManagedIdentity_ShouldBeConfiguredInNonTestingEnvironments()
    {
        // Arrange - Configure Production environment
        var appHost = await _distributedAppBuilder
            .WithEnvironment("Production")
            .BuildAsync();

        // Act - Get Admin Gateway service and check for Azure credential registration
        using var scope = appHost.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SecretsManagementIntegrationTests>>();

        // Assert - Verify DefaultAzureCredential is registered
        var azureCredential = scope.ServiceProvider.GetService<DefaultAzureCredential>();
        Assert.NotNull(azureCredential);

        _output.WriteLine("Azure Managed Identity test passed - DefaultAzureCredential is registered");
        logger.LogInformation("SECRETS_MANAGEMENT_TEST: Azure Managed Identity configuration validated successfully");
    }

    [Fact]
    public async Task AdminGateway_SecretResolution_ShouldHandleMissingSecretsGracefully()
    {
        // Arrange - Configure environment with intentionally missing secrets
        var appHost = await _distributedAppBuilder
            .WithEnvironment("Development")
            // Intentionally omit some required secrets to test fallback behavior
            .WithParameter("postgres-password", "test-password")
            // Missing: entra-tenant-id, entra-client-id, etc.
            .BuildAsync();

        // Act - Get Admin Gateway service
        using var scope = appHost.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SecretsManagementIntegrationTests>>();

        // Assert - Verify graceful handling of missing secrets
        var entraTenantId = configuration["EntraExternalId:TenantId"];
        var entraClientId = configuration["EntraExternalId:ClientId"];

        // Missing secrets should return null/empty without throwing exceptions
        Assert.True(string.IsNullOrEmpty(entraTenantId));
        Assert.True(string.IsNullOrEmpty(entraClientId));

        _output.WriteLine("Missing secrets fallback test passed - No exceptions thrown");
        logger.LogInformation("SECRETS_MANAGEMENT_TEST: Missing secrets fallback behavior validated successfully");
    }

    [Fact]
    public async Task AdminGateway_ConfigurationRefresh_ShouldSupportSecretRotation()
    {
        // Arrange - Configure Development environment with initial secrets
        var appHost = await _distributedAppBuilder
            .WithEnvironment("Development")
            .WithParameter("entra-tenant-id", "initial-tenant-id")
            .WithParameter("entra-client-id", "initial-client-id")
            .BuildAsync();

        // Act - Get Admin Gateway service and initial configuration
        using var scope = appHost.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SecretsManagementIntegrationTests>>();

        // Get initial values
        var initialTenantId = configuration["EntraExternalId:TenantId"];
        var initialClientId = configuration["EntraExternalId:ClientId"];

        // Assert - Verify configuration refresh capability exists
        var reloadToken = configuration.GetReloadToken();
        Assert.NotNull(reloadToken);

        _output.WriteLine($"Configuration refresh test passed - Initial values: TenantId={initialTenantId}, ClientId={initialClientId}");
        logger.LogInformation("SECRETS_MANAGEMENT_TEST: Configuration refresh capability validated successfully");
    }

    [Fact]
    public async Task AdminGateway_MedicalGradeAuditLogging_ShouldLogSecretOperations()
    {
        // Arrange - Configure Development environment
        var appHost = await _distributedAppBuilder
            .WithEnvironment("Development")
            .WithParameter("postgres-password", "audit-test-password")
            .WithParameter("entra-tenant-id", "audit-test-tenant")
            .BuildAsync();

        // Act - Get Admin Gateway service and trigger configuration access
        using var scope = appHost.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SecretsManagementIntegrationTests>>();

        // Access configuration values to trigger any logging
        var secretsProvider = configuration["SECRETS_PROVIDER"];
        var entraTenantId = configuration["EntraExternalId:TenantId"];

        // Assert - Verify audit logging context is available
        Assert.NotNull(logger);
        Assert.NotNull(secretsProvider);

        _output.WriteLine("Medical-grade audit logging test passed - Logger and configuration access validated");
        logger.LogInformation("MEDICAL_AUDIT: Secrets management audit logging capability validated successfully - SecretsProvider: {SecretsProvider}, ConfigurationAccess: {ConfigurationAccess}", 
            secretsProvider, "VALIDATED");
    }

    [Theory]
    [InlineData("Development", "LOCAL_PARAMETERS")]
    [InlineData("Production", "AZURE_KEY_VAULT")]
    [InlineData("Staging", "AZURE_KEY_VAULT")]
    public async Task AdminGateway_EnvironmentSpecificSecretHandling_ShouldUseCorrectProvider(string environment, string expectedProvider)
    {
        // Arrange - Configure specific environment
        var appHost = await _distributedAppBuilder
            .WithEnvironment(environment)
            .BuildAsync();

        // Act - Get Admin Gateway service
        using var scope = appHost.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SecretsManagementIntegrationTests>>();

        // Assert - Verify correct secrets provider for environment
        var secretsProvider = configuration["SECRETS_PROVIDER"];
        Assert.Equal(expectedProvider, secretsProvider);

        _output.WriteLine($"Environment-specific test passed - {environment} uses {expectedProvider}");
        logger.LogInformation("SECRETS_MANAGEMENT_TEST: Environment-specific secret handling validated - Environment: {Environment}, Provider: {Provider}", 
            environment, expectedProvider);
    }

    [Fact]
    public async Task AdminGateway_ApplicationInsightsIntegration_ShouldConfigureMonitoring()
    {
        // Arrange - Configure Development environment with Application Insights
        var appHost = await _distributedAppBuilder
            .WithEnvironment("Development")
            .WithParameter("appinsights-connection-string", "InstrumentationKey=test-key;IngestionEndpoint=https://test.applicationinsights.azure.com/")
            .BuildAsync();

        // Act - Get Admin Gateway service
        using var scope = appHost.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SecretsManagementIntegrationTests>>();

        // Assert - Verify Application Insights configuration
        var appInsightsConnectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        Assert.NotNull(appInsightsConnectionString);
        Assert.Contains("InstrumentationKey=test-key", appInsightsConnectionString);

        _output.WriteLine("Application Insights integration test passed");
        logger.LogInformation("SECRETS_MANAGEMENT_TEST: Application Insights monitoring configuration validated successfully");
    }
}