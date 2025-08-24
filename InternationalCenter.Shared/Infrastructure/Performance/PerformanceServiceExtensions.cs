using InternationalCenter.Shared.Infrastructure.Caching;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace InternationalCenter.Shared.Infrastructure.Performance;

public static class PerformanceServiceExtensions
{
    public static IServiceCollection AddPerformanceOptimizations(
        this IServiceCollection services, 
        IHostEnvironment environment)
    {
        // Distributed caching with Redis
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = "localhost:6379"; // Will be overridden by Aspire
            options.InstanceName = "InternationalCenter";
        });

        // Register cache services
        services.AddSingleton<ICacheKeyService, CacheKeyService>();
        services.AddScoped<ICacheService, RedisCacheService>();

        // Response compression
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
            options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
            
            options.MimeTypes = new[]
            {
                "text/plain",
                "text/html",
                "text/css",
                "text/javascript",
                "application/javascript",
                "application/json",
                "application/xml",
                "text/xml"
            };
        });

        // Response caching
        services.AddResponseCaching(options =>
        {
            options.MaximumBodySize = 64 * 1024 * 1024; // 64 MB
            options.UseCaseSensitivePaths = false;
        });

        // Memory caching for in-process caching
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = environment.IsDevelopment() ? 100 : 1000;
            options.CompactionPercentage = 0.25;
        });

        // Output caching for ASP.NET Core
        services.AddOutputCache(options =>
        {
            options.AddBasePolicy(builder => 
                builder.Expire(TimeSpan.FromMinutes(5)));
            
            // Short-term policy for dynamic content
            options.AddPolicy("ShortTerm", builder =>
                builder.Expire(TimeSpan.FromMinutes(5))
                       .VaryByValue(context => 
                           new KeyValuePair<string, string>("headers", 
                               $"{context.Request.Headers.Accept}_{context.Request.Headers.AcceptEncoding}")));
            
            // Medium-term policy for semi-static content
            options.AddPolicy("MediumTerm", builder =>
                builder.Expire(TimeSpan.FromMinutes(30))
                       .VaryByValue(context => 
                           new KeyValuePair<string, string>("headers", 
                               $"{context.Request.Headers.Accept}_{context.Request.Headers.AcceptEncoding}")));
            
            // Long-term policy for static content
            options.AddPolicy("LongTerm", builder =>
                builder.Expire(TimeSpan.FromHours(1))
                       .VaryByValue(context => 
                           new KeyValuePair<string, string>("encoding", 
                               context.Request.Headers.AcceptEncoding.ToString())));
        });

        // Register middleware configurations
        services.AddSingleton<ResponseCompressionOptions>();
        services.AddSingleton<ResponseCachingOptions>();

        return services;
    }

    public static IApplicationBuilder UsePerformanceOptimizations(
        this IApplicationBuilder app,
        IHostEnvironment environment)
    {
        // Response compression should be early in pipeline
        app.UseResponseCompression();

        // Custom response caching middleware
        app.UseMiddleware<ResponseCachingMiddleware>();

        // Built-in response caching
        app.UseResponseCaching();

        // Output caching
        app.UseOutputCache();

        // Custom compression middleware for production optimization
        if (!environment.IsDevelopment())
        {
            app.UseMiddleware<ResponseCompressionMiddleware>();
        }

        return app;
    }

    public static IServiceCollection AddRedisConnection(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var configuration = ConfigurationOptions.Parse(connectionString);
            configuration.AbortOnConnectFail = false;
            configuration.ConnectRetry = 3;
            configuration.ConnectTimeout = 5000;
            configuration.SyncTimeout = 5000;
            
            return ConnectionMultiplexer.Connect(configuration);
        });

        return services;
    }

    // Note: Caching strategies are implemented at the service level
    // Each API project handles its own cached decorators
}

// Extension method for Decorator pattern (requires Scrutor package)
public static class ServiceCollectionDecorationExtensions
{
    public static IServiceCollection Decorate<TInterface, TImplementation>(this IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        // Simple decorator implementation without external dependencies
        var existingService = services.FirstOrDefault(s => s.ServiceType == typeof(TInterface));
        if (existingService != null)
        {
            services.Remove(existingService);
            services.Add(new ServiceDescriptor(
                typeof(TInterface),
                provider => ActivatorUtilities.CreateInstance<TImplementation>(provider, 
                    ActivatorUtilities.CreateInstance(provider, existingService.ImplementationType!)),
                existingService.Lifetime));
        }

        return services;
    }
}