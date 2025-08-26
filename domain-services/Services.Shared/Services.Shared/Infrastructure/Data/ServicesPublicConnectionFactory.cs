using Infrastructure.Database.Abstractions;
using Infrastructure.Database.Base;
using Infrastructure.Database.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data;
using Dapper;

namespace Services.Shared.Infrastructure.Data;

/// <summary>
/// Services domain-specific Public API database connection factory extending generic Dapper infrastructure.
/// DOMAIN: Services domain specific database operations
/// PUBLIC API: High-performance Dapper implementation for Services Public API operations  
/// READ-ONLY: Optimized for high-throughput read operations
/// </summary>
public sealed class ServicesPublicConnectionFactory : BaseDapperConnectionFactory, IDbConnectionFactory
{
    private readonly ILogger<ServicesPublicConnectionFactory> _logger;

    public ServicesPublicConnectionFactory(
        IOptions<DatabaseConnectionOptions> options,
        ILogger<ServicesPublicConnectionFactory> logger) 
        : base(options, logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get optimized database connection for Services Public API operations.
    /// PUBLIC API: Read-optimized connection configuration
    /// </summary>
    public override async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = await base.CreateConnectionAsync(cancellationToken);
        
        // Configure connection for Services domain read optimization
        await ConfigureServicesPublicConnectionAsync(connection, cancellationToken);
        
        return connection;
    }

    /// <summary>
    /// Configure connection specifically for Services Public API performance.
    /// PERFORMANCE: Services domain read optimizations
    /// </summary>
    private async Task ConfigureServicesPublicConnectionAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            // Set PostgreSQL connection parameters optimized for Services public operations
            var configCommands = new[]
            {
                // Optimize for read-heavy workloads typical of Public APIs
                "SET SESSION default_transaction_isolation = 'read committed'",
                "SET SESSION statement_timeout = '30s'",
                "SET SESSION idle_in_transaction_session_timeout = '60s'",
                
                // Services domain specific optimizations
                "SET SESSION work_mem = '4MB'", // Increased for Services queries with JOINs
                "SET SESSION random_page_cost = 1.1", // SSD optimization for Services data
                
                // Enable query plan caching for frequent Services queries
                "SET SESSION plan_cache_mode = 'force_generic_plan'"
            };

            foreach (var command in configCommands)
            {
                await connection.ExecuteAsync(command);
            }

            _logger?.LogDebug("Services Public API connection configured for optimal performance");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to configure Services Public API connection optimizations");
            // Continue without optimizations - connection is still functional
        }
    }

    /// <summary>
    /// Execute Services domain-specific read query with audit logging.
    /// PUBLIC API: Services read operations with audit
    /// </summary>
    public async Task<T> ExecuteServicesQueryAsync<T>(
        string sql,
        object? parameters = null,
        string? operationName = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            var result = await ExecuteQueryAsync<T>(sql, parameters, cancellationToken);
            
            // Log successful Services operation
            var duration = DateTime.UtcNow - startTime;
            _logger?.LogDebug("SERVICES PUBLIC QUERY: Operation={Operation}, Duration={Duration}ms, SQL={SQL}",
                operationName ?? "UnnamedQuery", duration.TotalMilliseconds, sql);
            
            return result;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger?.LogError(ex, "SERVICES PUBLIC QUERY FAILED: Operation={Operation}, Duration={Duration}ms, SQL={SQL}",
                operationName ?? "UnnamedQuery", duration.TotalMilliseconds, sql);
            throw;
        }
    }

    /// <summary>
    /// Execute Services domain-specific read queries with audit logging.
    /// PUBLIC API: Services bulk read operations with audit
    /// </summary>
    public async Task<IEnumerable<T>> ExecuteServicesQueriesAsync<T>(
        string sql,
        object? parameters = null,
        string? operationName = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            var results = await ExecuteQueriesAsync<T>(sql, parameters, cancellationToken);
            var resultCount = results.Count();
            
            // Log successful Services operation
            var duration = DateTime.UtcNow - startTime;
            _logger?.LogDebug("SERVICES PUBLIC QUERIES: Operation={Operation}, Duration={Duration}ms, Count={Count}, SQL={SQL}",
                operationName ?? "UnnamedQueries", duration.TotalMilliseconds, resultCount, sql);
            
            return results;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger?.LogError(ex, "SERVICES PUBLIC QUERIES FAILED: Operation={Operation}, Duration={Duration}ms, SQL={SQL}",
                operationName ?? "UnnamedQueries", duration.TotalMilliseconds, sql);
            throw;
        }
    }

    /// <summary>
    /// Get published services optimized for Public API consumption.
    /// PUBLIC API: High-performance services retrieval
    /// </summary>
    public async Task<IEnumerable<dynamic>> GetPublishedServicesAsync(
        int? limit = null,
        int offset = 0,
        bool featuredOnly = false,
        string? categorySlug = null,
        CancellationToken cancellationToken = default)
    {
        var sql = $@"
            SELECT 
                s.""Id"",
                s.""Title"",
                s.""Slug"",
                s.""Description"",
                s.""icon"",
                s.""image"",
                s.""Available"",
                s.""Featured"",
                s.""priority"",
                s.""technologies"",
                s.""features"",
                s.""delivery_modes"",
                s.""meta_title"",
                s.""meta_description"",
                sc.""Name"" as ""CategoryName"",
                sc.""Slug"" as ""CategorySlug""
            FROM ""services"" s
            LEFT JOIN ""service_categories"" sc ON s.""CategoryId"" = sc.""Id""
            WHERE s.""Status"" = 'Published'
                AND s.""Available"" = true
                {(featuredOnly ? "AND s.\"Featured\" = true" : "")}
                {(categorySlug != null ? "AND sc.\"Slug\" = @CategorySlug" : "")}
            ORDER BY s.""Featured"" DESC, s.""priority"" ASC, s.""Title"" ASC
            {(limit.HasValue ? "LIMIT @Limit" : "")}
            {(offset > 0 ? "OFFSET @Offset" : "")}";

        var parameters = new
        {
            CategorySlug = categorySlug,
            Limit = limit,
            Offset = offset
        };

        return await ExecuteServicesQueriesAsync<dynamic>(
            sql, 
            parameters, 
            "GetPublishedServices", 
            cancellationToken);
    }

    /// <summary>
    /// Get service by slug optimized for Public API consumption.
    /// PUBLIC API: Single service retrieval by slug
    /// </summary>
    public async Task<dynamic?> GetServiceBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                s.""Id"",
                s.""Title"",
                s.""Slug"",
                s.""Description"",
                s.""DetailedDescription"",
                s.""icon"",
                s.""image"",
                s.""Available"",
                s.""Featured"",
                s.""technologies"",
                s.""features"",
                s.""delivery_modes"",
                s.""meta_title"",
                s.""meta_description"",
                sc.""Name"" as ""CategoryName"",
                sc.""Slug"" as ""CategorySlug"",
                sc.""Description"" as ""CategoryDescription""
            FROM ""services"" s
            LEFT JOIN ""service_categories"" sc ON s.""CategoryId"" = sc.""Id""
            WHERE s.""Slug"" = @Slug 
                AND s.""Status"" = 'Published'
                AND s.""Available"" = true
            LIMIT 1";

        return await ExecuteServicesQueryAsync<dynamic?>(
            sql,
            new { Slug = slug },
            "GetServiceBySlug",
            cancellationToken);
    }

    /// <summary>
    /// Get active service categories optimized for Public API consumption.
    /// PUBLIC API: Service categories retrieval
    /// </summary>
    public async Task<IEnumerable<dynamic>> GetActiveServiceCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                sc.""Id"",
                sc.""Name"",
                sc.""Slug"",
                sc.""Description"",
                sc.""DisplayOrder"",
                COUNT(s.""Id"") as ""ServiceCount""
            FROM ""service_categories"" sc
            LEFT JOIN ""services"" s ON sc.""Id"" = s.""CategoryId"" 
                AND s.""Status"" = 'Published' 
                AND s.""Available"" = true
            WHERE sc.""Active"" = true
            GROUP BY sc.""Id"", sc.""Name"", sc.""Slug"", sc.""Description"", sc.""DisplayOrder""
            ORDER BY sc.""DisplayOrder"" ASC, sc.""Name"" ASC";

        return await ExecuteServicesQueriesAsync<dynamic>(
            sql,
            null,
            "GetActiveServiceCategories", 
            cancellationToken);
    }

    /// <summary>
    /// Get featured services optimized for Public API homepage.
    /// PUBLIC API: Featured services for homepage display
    /// </summary>
    public async Task<IEnumerable<dynamic>> GetFeaturedServicesAsync(
        int limit = 6,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                s.""Id"",
                s.""Title"",
                s.""Slug"",
                s.""Description"",
                s.""icon"",
                s.""image"",
                s.""priority"",
                sc.""Name"" as ""CategoryName"",
                sc.""Slug"" as ""CategorySlug""
            FROM ""services"" s
            LEFT JOIN ""service_categories"" sc ON s.""CategoryId"" = sc.""Id""
            WHERE s.""Status"" = 'Published'
                AND s.""Available"" = true
                AND s.""Featured"" = true
            ORDER BY s.""priority"" ASC, s.""Title"" ASC
            LIMIT @Limit";

        return await ExecuteServicesQueriesAsync<dynamic>(
            sql,
            new { Limit = limit },
            "GetFeaturedServices",
            cancellationToken);
    }
}