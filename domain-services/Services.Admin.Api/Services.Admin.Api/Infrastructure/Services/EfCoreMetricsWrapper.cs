using Microsoft.EntityFrameworkCore;
using Services.Admin.Api.Infrastructure.Services;
using System.Diagnostics;

namespace Services.Admin.Api.Infrastructure.Services;

/// <summary>
/// EF Core metrics wrapper for automatic performance tracking and medical-grade compliance
/// Wraps DbContext operations to collect comprehensive metrics for Services Admin API
/// </summary>
public sealed class EfCoreMetricsWrapper<TContext> : IDisposable where TContext : DbContext
{
    private readonly TContext _context;
    private readonly ServicesAdminApiMetricsService _metricsService;
    private readonly ILogger<EfCoreMetricsWrapper<TContext>> _logger;
    
    public EfCoreMetricsWrapper(TContext context, ServicesAdminApiMetricsService metricsService, ILogger<EfCoreMetricsWrapper<TContext>> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Hook into EF Core events for automatic metrics collection
        RegisterEfCoreEvents();
    }

    public TContext Context => _context;

    private void RegisterEfCoreEvents()
    {
        // Track change tracking performance
        _context.ChangeTracker.Tracked += (sender, args) =>
        {
            var trackedEntitiesCount = _context.ChangeTracker.Entries().Count();
            _metricsService.UpdateTrackedEntitiesCount(trackedEntitiesCount);
        };

        _context.ChangeTracker.StateChanged += (sender, args) =>
        {
            var trackedEntitiesCount = _context.ChangeTracker.Entries().Count();
            _metricsService.UpdateTrackedEntitiesCount(trackedEntitiesCount);
        };

        _logger.LogDebug("EF Core metrics wrapper registered events for context: {ContextType}", typeof(TContext).Name);
    }

    public async Task<int> SaveChangesWithMetricsAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var changeTrackingStopwatch = Stopwatch.StartNew();
        
        var changedEntries = _context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .ToList();
        
        changeTrackingStopwatch.Stop();
        var changeTrackingTimeMs = (int)changeTrackingStopwatch.ElapsedMilliseconds;
        
        var affectedEntities = changedEntries.Count;
        var success = false;
        
        try
        {
            var result = await _context.SaveChangesAsync(cancellationToken);
            stopwatch.Stop();
            success = true;
            
            _metricsService.RecordEfCoreSaveChanges(
                stopwatch.Elapsed.TotalSeconds, 
                success, 
                affectedEntities, 
                changeTrackingTimeMs);
                
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            success = false;
            
            _metricsService.RecordEfCoreSaveChanges(
                stopwatch.Elapsed.TotalSeconds, 
                success, 
                affectedEntities, 
                changeTrackingTimeMs);
                
            // Track specific database errors
            if (ex.Message.Contains("deadlock", StringComparison.OrdinalIgnoreCase))
            {
                _metricsService.RecordDatabaseTransaction("save_changes", stopwatch.Elapsed.TotalSeconds, false, deadlockDetected: true);
            }
            else if (ex.Message.Contains("conflict", StringComparison.OrdinalIgnoreCase) || 
                     ex.Message.Contains("concurrency", StringComparison.OrdinalIgnoreCase))
            {
                _metricsService.RecordDatabaseTransaction("save_changes", stopwatch.Elapsed.TotalSeconds, false, concurrencyConflict: true);
            }
            
            _logger.LogError(ex, "EF Core SaveChanges failed with metrics: affectedEntities={AffectedEntities}, duration={Duration}ms",
                affectedEntities, stopwatch.ElapsedMilliseconds);
                
            throw;
        }
    }

    public async Task<T?> ExecuteQueryWithMetricsAsync<T>(
        string queryName, 
        Func<TContext, CancellationToken, Task<T?>> queryFunc,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        var resultCount = 0;
        
        try
        {
            var result = await queryFunc(_context, cancellationToken);
            stopwatch.Stop();
            success = true;
            
            // Attempt to count results for collections
            if (result is IEnumerable<object> collection)
            {
                resultCount = collection.Count();
            }
            else if (result != null)
            {
                resultCount = 1;
            }
            
            _metricsService.RecordEfCoreQuery(queryName, stopwatch.Elapsed.TotalSeconds, success, resultCount);
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            success = false;
            
            _metricsService.RecordEfCoreQuery(queryName, stopwatch.Elapsed.TotalSeconds, success, resultCount);
            
            _logger.LogError(ex, "EF Core query failed with metrics: queryName={QueryName}, duration={Duration}ms",
                queryName, stopwatch.ElapsedMilliseconds);
                
            throw;
        }
    }

    public async Task ExecuteTransactionWithMetricsAsync(
        string operationType,
        Func<TContext, CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var committed = false;
        var deadlockDetected = false;
        var concurrencyConflict = false;
        
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            await operation(_context, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            stopwatch.Stop();
            committed = true;
            
            _metricsService.RecordDatabaseTransaction(operationType, stopwatch.Elapsed.TotalSeconds, committed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // Analyze exception for specific database errors
            var exceptionMessage = ex.Message.ToLowerInvariant();
            deadlockDetected = exceptionMessage.Contains("deadlock");
            concurrencyConflict = exceptionMessage.Contains("conflict") || exceptionMessage.Contains("concurrency");
            
            try
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Failed to rollback transaction during exception handling");
            }
            
            _metricsService.RecordDatabaseTransaction(operationType, stopwatch.Elapsed.TotalSeconds, committed, deadlockDetected, concurrencyConflict);
            
            _logger.LogError(ex, "Database transaction failed: operationType={OperationType}, duration={Duration}ms, deadlock={Deadlock}, concurrency={ConcurrencyConflict}",
                operationType, stopwatch.ElapsedMilliseconds, deadlockDetected, concurrencyConflict);
                
            throw;
        }
    }

    public void RecordDataMutation(string operationType, string entityType, string entityId, string userId)
    {
        // This method is typically called from business logic layer
        var stopwatch = Stopwatch.StartNew();
        stopwatch.Stop(); // Immediate call since we're just recording the operation type
        
        _metricsService.RecordDataMutation(operationType, entityType, entityId, userId, stopwatch.Elapsed.TotalSeconds, true);
    }

    public void Dispose()
    {
        // Note: We don't dispose the context here as it's managed by DI container
        _logger.LogDebug("EF Core metrics wrapper disposed for context: {ContextType}", typeof(TContext).Name);
    }
}