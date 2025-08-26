using Microsoft.Extensions.Azure;

namespace Infrastructure.SecretStore.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSecretStore(this IServiceCollection services, 
        IConfiguration configuration)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        // Configure options
        services.Configure<SecretStoreOptions>(configuration.GetSection(SecretStoreOptions.SectionName));
        services.AddSingleton<IValidator<SecretStoreOptions>, SecretStoreOptionsValidator>();

        // Add memory cache for secret caching
        services.AddMemoryCache();

        // Configure Azure clients based on options
        var options = configuration.GetSection(SecretStoreOptions.SectionName).Get<SecretStoreOptions>();
        
        if (options?.Enabled == true && options.Provider == SecretStoreProvider.AzureKeyVault)
        {
            ConfigureAzureKeyVault(services, options);
        }

        // Register secret store services
        services.AddScoped<IKeyVaultManager, AzureKeyVaultManager>();
        services.AddScoped<ISecretStore, Services.SecretStore>();
        services.AddSingleton<ISecretRotationService, SecretRotationService>();

        // Add health checks for Key Vault
        if (options?.EnableHealthChecks == true)
        {
            services.AddHealthChecks()
                .AddCheck<KeyVaultHealthCheck>("keyvault", 
                    tags: new[] { "keyvault", "secrets", "readiness" });
        }

        return services;
    }

    public static IServiceCollection AddSecretStore(this IServiceCollection services, 
        IConfiguration configuration, Action<SecretStoreOptions> configureOptions)
    {
        if (configureOptions == null)
        {
            throw new ArgumentNullException(nameof(configureOptions));
        }

        services.AddSecretStore(configuration);
        services.Configure(configureOptions);

        return services;
    }

    private static void ConfigureAzureKeyVault(IServiceCollection services, SecretStoreOptions options)
    {
        if (string.IsNullOrEmpty(options.VaultUri))
        {
            throw new InvalidOperationException("VaultUri is required for Azure Key Vault configuration");
        }

        var vaultUri = new Uri(options.VaultUri);

        services.AddAzureClients(clientBuilder =>
        {
            // Configure credential based on authentication options
            var credential = CreateCredential(options.Authentication);

            // Add Key Vault clients
            clientBuilder.AddSecretClient(vaultUri)
                .WithCredential(credential)
                .ConfigureOptions(clientOptions =>
                {
                    clientOptions.Retry.MaxRetries = options.MaxRetryAttempts;
                    clientOptions.Retry.Delay = options.RetryDelay;
                    clientOptions.Retry.MaxDelay = options.RetryDelay * 3;
                });

            clientBuilder.AddKeyClient(vaultUri)
                .WithCredential(credential)
                .ConfigureOptions(clientOptions =>
                {
                    clientOptions.Retry.MaxRetries = options.MaxRetryAttempts;
                    clientOptions.Retry.Delay = options.RetryDelay;
                    clientOptions.Retry.MaxDelay = options.RetryDelay * 3;
                });

            clientBuilder.AddCertificateClient(vaultUri)
                .WithCredential(credential)
                .ConfigureOptions(clientOptions =>
                {
                    clientOptions.Retry.MaxRetries = options.MaxRetryAttempts;
                    clientOptions.Retry.Delay = options.RetryDelay;
                    clientOptions.Retry.MaxDelay = options.RetryDelay * 3;
                });
        });
    }

    private static TokenCredential CreateCredential(AuthenticationOptions authOptions)
    {
        return authOptions.Provider switch
        {
            AuthenticationProvider.ManagedIdentity => CreateManagedIdentityCredential(authOptions),
            AuthenticationProvider.ServicePrincipal => CreateServicePrincipalCredential(authOptions),
            AuthenticationProvider.Certificate => CreateCertificateCredential(authOptions),
            AuthenticationProvider.DefaultAzureCredential => CreateDefaultAzureCredential(authOptions),
            AuthenticationProvider.Interactive => new InteractiveBrowserCredential(),
            _ => throw new NotSupportedException($"Authentication provider {authOptions.Provider} is not supported")
        };
    }

    private static TokenCredential CreateManagedIdentityCredential(AuthenticationOptions authOptions)
    {
        var options = new ManagedIdentityCredentialOptions();
        
        if (!string.IsNullOrEmpty(authOptions.ClientId))
        {
            options.ClientId = authOptions.ClientId;
        }

        return new ManagedIdentityCredential(options);
    }

    private static TokenCredential CreateServicePrincipalCredential(AuthenticationOptions authOptions)
    {
        if (string.IsNullOrEmpty(authOptions.TenantId) || 
            string.IsNullOrEmpty(authOptions.ClientId) || 
            string.IsNullOrEmpty(authOptions.ClientSecret))
        {
            throw new InvalidOperationException("TenantId, ClientId, and ClientSecret are required for Service Principal authentication");
        }

        var options = new ClientSecretCredentialOptions();
        
        if (!string.IsNullOrEmpty(authOptions.Authority))
        {
            options.AuthorityHost = new Uri(authOptions.Authority);
        }

        if (authOptions.AdditionallyAllowedTenants?.Length > 0)
        {
            options.AdditionallyAllowedTenants.Clear();
            foreach (var tenant in authOptions.AdditionallyAllowedTenants)
            {
                options.AdditionallyAllowedTenants.Add(tenant);
            }
        }

        options.DisableInstanceDiscovery = authOptions.DisableInstanceDiscovery;

        return new ClientSecretCredential(authOptions.TenantId, authOptions.ClientId, authOptions.ClientSecret, options);
    }

    private static TokenCredential CreateCertificateCredential(AuthenticationOptions authOptions)
    {
        if (string.IsNullOrEmpty(authOptions.TenantId) || string.IsNullOrEmpty(authOptions.ClientId))
        {
            throw new InvalidOperationException("TenantId and ClientId are required for Certificate authentication");
        }

        var options = new ClientCertificateCredentialOptions();
        
        if (!string.IsNullOrEmpty(authOptions.Authority))
        {
            options.AuthorityHost = new Uri(authOptions.Authority);
        }

        options.DisableInstanceDiscovery = authOptions.DisableInstanceDiscovery;

        // Load certificate from store or file
        if (!string.IsNullOrEmpty(authOptions.CertificateThumbprint))
        {
            // Load from certificate store
            using var store = new System.Security.Cryptography.X509Certificates.X509Store(
                System.Security.Cryptography.X509Certificates.StoreName.My,
                System.Security.Cryptography.X509Certificates.StoreLocation.CurrentUser);
            
            store.Open(System.Security.Cryptography.X509Certificates.OpenFlags.ReadOnly);
            var certificates = store.Certificates.Find(
                System.Security.Cryptography.X509Certificates.X509FindType.FindByThumbprint,
                authOptions.CertificateThumbprint, false);

            if (certificates.Count == 0)
            {
                throw new InvalidOperationException($"Certificate with thumbprint {authOptions.CertificateThumbprint} not found");
            }

            return new ClientCertificateCredential(authOptions.TenantId, authOptions.ClientId, certificates[0], options);
        }
        else if (!string.IsNullOrEmpty(authOptions.CertificatePath))
        {
            // Load from file
            var certificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(authOptions.CertificatePath);
            return new ClientCertificateCredential(authOptions.TenantId, authOptions.ClientId, certificate, options);
        }
        else
        {
            throw new InvalidOperationException("Either CertificateThumbprint or CertificatePath must be provided for Certificate authentication");
        }
    }

    private static TokenCredential CreateDefaultAzureCredential(AuthenticationOptions authOptions)
    {
        var options = new DefaultAzureCredentialOptions();
        
        if (!string.IsNullOrEmpty(authOptions.TenantId))
        {
            options.VisualStudioTenantId = authOptions.TenantId;
            options.VisualStudioCodeTenantId = authOptions.TenantId;
        }

        if (!string.IsNullOrEmpty(authOptions.ClientId))
        {
            options.ManagedIdentityClientId = authOptions.ClientId;
        }

        options.ExcludeInteractiveBrowserCredential = true; // Typically not wanted in production
        
        return new DefaultAzureCredential(options);
    }
}