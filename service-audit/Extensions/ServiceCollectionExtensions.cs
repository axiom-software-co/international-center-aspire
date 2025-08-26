using Infrastructure.Database.Extensions;
using Microsoft.EntityFrameworkCore;
using Service.Audit.Data;

namespace Service.Audit.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuditServices(this IServiceCollection services, 
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
        services.Configure<AuditServiceOptions>(configuration.GetSection(AuditServiceOptions.SectionName));
        services.AddSingleton<IValidator<AuditServiceOptions>, AuditServiceOptionsValidator>();

        // Configure database
        services.AddDatabaseInfrastructure(configuration);
        
        services.AddDbContext<AuditDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });

            options.EnableSensitiveDataLogging(false);
            options.EnableServiceProviderCaching(true);
            options.EnableDetailedErrors(true);
        });

        // Register audit services
        services.AddScoped<IAuditRepository, EfCoreAuditRepository>();
        services.AddScoped<IAuditSigningService, HmacAuditSigningService>();
        services.AddScoped<IAuditService, Services.AuditService>();

        // Register HTTP context accessor for audit context
        services.AddHttpContextAccessor();

        return services;
    }

    public static IServiceCollection AddAuditServices(this IServiceCollection services, 
        IConfiguration configuration, Action<AuditServiceOptions> configureOptions)
    {
        if (configureOptions == null)
        {
            throw new ArgumentNullException(nameof(configureOptions));
        }

        services.AddAuditServices(configuration);
        services.Configure(configureOptions);

        return services;
    }
}