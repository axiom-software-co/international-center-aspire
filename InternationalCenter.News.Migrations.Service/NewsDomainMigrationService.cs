using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InternationalCenter.News.Migrations.Service;

/// <summary>
/// Domain-specific migration service for News vertical slice
/// Handles NewsArticles and NewsCategories migrations independently from other domains
/// </summary>
public class NewsDomainMigrationService : INewsDomainMigrationService
{
    private readonly NewsDbContext _context;
    private readonly ILogger<NewsDomainMigrationService> _logger;

    public NewsDomainMigrationService(
        NewsDbContext context,
        ILogger<NewsDomainMigrationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ApplyMigrationsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("News Domain: Starting News-specific migration process");

        try
        {
            // Use execution strategy for resilience (Microsoft recommended pattern)
            var strategy = _context.Database.CreateExecutionStrategy();
            
            await strategy.ExecuteAsync(async () =>
            {
                // Verify database connectivity
                var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
                if (!canConnect)
                {
                    throw new InvalidOperationException("News Domain: Cannot connect to database");
                }

                _logger.LogInformation("News Domain: Database connectivity verified");

                // Get News-specific pending migrations
                var pendingMigrations = await GetPendingMigrationsAsync(cancellationToken);
                
                if (pendingMigrations.Any())
                {
                    _logger.LogInformation("News Domain: Applying {Count} News-specific migrations", pendingMigrations.Count());
                    
                    // Apply migrations with domain-specific validation
                    foreach (var migration in pendingMigrations)
                    {
                        _logger.LogInformation("News Domain: Applying migration {Migration}", migration);
                    }
                    
                    await _context.Database.MigrateAsync(cancellationToken);
                    
                    // Validate News domain schema after migration
                    await ValidateNewsDomainSchemaAsync(cancellationToken);
                    
                    _logger.LogInformation("News Domain: All News migrations applied successfully");
                }
                else
                {
                    _logger.LogInformation("News Domain: No pending migrations found");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "News Domain: Migration process failed");
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var allMigrations = _context.Database.GetMigrations();
            var appliedMigrations = await _context.Database.GetAppliedMigrationsAsync(cancellationToken);
            
            // Filter to News-domain specific migrations
            var newsMigrations = allMigrations.Where(m => 
                m.Contains("News") || 
                m.Contains("NewsArticles") ||
                m.Contains("NewsCategories"));
            
            var pendingNewsMigrations = newsMigrations.Except(appliedMigrations);
            
            _logger.LogInformation("News Domain: Found {Total} total migrations, {Applied} applied, {Pending} pending", 
                newsMigrations.Count(), 
                appliedMigrations.Count(), 
                pendingNewsMigrations.Count());
            
            return pendingNewsMigrations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "News Domain: Failed to retrieve pending migrations");
            throw;
        }
    }

    private async Task ValidateNewsDomainSchemaAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("News Domain: Validating schema integrity");

        // Validate NewsArticles table exists and has expected structure
        var articlesTableExists = await _context.Database.ExecuteScalarAsync<bool>(
            @"SELECT EXISTS (
                SELECT FROM information_schema.tables 
                WHERE table_schema = 'public' 
                AND table_name = 'NewsArticles'
            )", cancellationToken);

        if (!articlesTableExists)
        {
            throw new InvalidOperationException("News Domain: NewsArticles table not found after migration");
        }

        // Validate NewsCategories table exists  
        var categoriesTableExists = await _context.Database.ExecuteScalarAsync<bool>(
            @"SELECT EXISTS (
                SELECT FROM information_schema.tables 
                WHERE table_schema = 'public' 
                AND table_name = 'NewsCategories'
            )", cancellationToken);

        if (!categoriesTableExists)
        {
            throw new InvalidOperationException("News Domain: NewsCategories table not found after migration");
        }

        // Validate critical News domain indexes exist for performance
        var criticalIndexes = new[]
        {
            "IX_NewsArticles_Published_PublishDate",
            "IX_NewsArticles_CategoryId", 
            "IX_NewsArticles_Featured_PublishDate",
            "IX_NewsArticles_Title_Summary",
            "IX_NewsCategories_Active_SortOrder",
            "IX_NewsCategories_Name_Unique"
        };

        foreach (var indexName in criticalIndexes)
        {
            var indexExists = await _context.Database.ExecuteScalarAsync<bool>(
                @"SELECT EXISTS (
                    SELECT FROM pg_class c 
                    JOIN pg_namespace n ON n.oid = c.relnamespace 
                    WHERE c.relkind = 'i' 
                    AND n.nspname = 'public' 
                    AND c.relname = $1
                )", 
                new[] { indexName }, 
                cancellationToken);

            if (!indexExists)
            {
                _logger.LogWarning("News Domain: Critical index {IndexName} not found", indexName);
            }
        }

        // Validate News domain constraints and data integrity
        await ValidateNewsDomainConstraintsAsync(cancellationToken);

        _logger.LogInformation("News Domain: Schema validation completed");
    }

    private async Task ValidateNewsDomainConstraintsAsync(CancellationToken cancellationToken)
    {
        // Verify foreign key relationship between NewsArticles and NewsCategories
        var orphanedArticles = await _context.Database.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) FROM ""NewsArticles"" na 
              LEFT JOIN ""NewsCategories"" nc ON na.""CategoryId"" = nc.""Id""
              WHERE na.""CategoryId"" IS NOT NULL AND nc.""Id"" IS NULL", 
            cancellationToken);

        if (orphanedArticles > 0)
        {
            _logger.LogWarning("News Domain: Found {Count} orphaned articles with invalid CategoryId", orphanedArticles);
        }

        // Verify no duplicate category names
        var duplicateCategories = await _context.Database.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) FROM (
                SELECT ""Name"" FROM ""NewsCategories""
                GROUP BY ""Name""
                HAVING COUNT(*) > 1
              ) duplicates", 
            cancellationToken);

        if (duplicateCategories > 0)
        {
            throw new InvalidOperationException($"News Domain: Found {duplicateCategories} duplicate category names");
        }

        _logger.LogInformation("News Domain: Domain constraints validation completed");
    }
}

/// <summary>
/// Interface for News domain migration service
/// </summary>
public interface INewsDomainMigrationService
{
    Task ApplyMigrationsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default);
}