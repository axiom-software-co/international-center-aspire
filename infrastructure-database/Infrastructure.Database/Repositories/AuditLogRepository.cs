using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure;
using Shared.Models;
using Shared.Repositories;

namespace Infrastructure.Database.Repositories;

/// <summary>
/// Medical-grade audit log repository implementation using EF Core
/// Zero data loss compliance with PostgreSQL optimizations
/// Leverages comprehensive indexing strategy for high-performance queries
/// </summary>
public class AuditLogRepository : IAuditLogRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditLogRepository> _logger;

    public AuditLogRepository(ApplicationDbContext context, ILogger<AuditLogRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> CreateAuditLogAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Medical-grade audit log created successfully for {EntityType}:{EntityId} by {UserId}", 
                auditLog.EntityType, auditLog.EntityId, auditLog.UserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create medical-grade audit log for {EntityType}:{EntityId}", 
                auditLog.EntityType, auditLog.EntityId);
            return false;
        }
    }

    public async Task<bool> CreateAuditLogsBatchAsync(IEnumerable<AuditLog> auditLogs, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.AuditLogs.AddRange(auditLogs);
            var savedCount = await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Medical-grade audit batch operation completed: {SavedCount} audit logs created", savedCount);
            return savedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create medical-grade audit log batch operation");
            return false;
        }
    }

    public async Task<AuditLog?> GetAuditLogByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsByEntityAsync(string entityType, string entityId, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.AuditTimestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsByUserAsync(string userId, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.AuditTimestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.CorrelationId == correlationId)
            .OrderBy(a => a.AuditTimestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetCriticalAuditLogsAsync(DateTime fromDate, DateTime toDate, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.IsCriticalAction && a.AuditTimestamp >= fromDate && a.AuditTimestamp <= toDate)
            .OrderByDescending(a => a.AuditTimestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsByDateRangeAsync(DateTime fromDate, DateTime toDate, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.AuditTimestamp >= fromDate && a.AuditTimestamp <= toDate)
            .OrderByDescending(a => a.AuditTimestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsByActionAsync(string action, DateTime fromDate, DateTime toDate, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.Action == action && a.AuditTimestamp >= fromDate && a.AuditTimestamp <= toDate)
            .OrderByDescending(a => a.AuditTimestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetAuditLogsCountAsync(string? entityType = null, string? action = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(a => a.EntityType == entityType);

        if (!string.IsNullOrEmpty(action))
            query = query.Where(a => a.Action == action);

        if (fromDate.HasValue)
            query = query.Where(a => a.AuditTimestamp >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(a => a.AuditTimestamp <= toDate.Value);

        return await query.CountAsync(cancellationToken);
    }

    public async Task<int> DeleteExpiredAuditLogsAsync(DateTime cutoffDate, bool criticalActionsOnly = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = criticalActionsOnly 
                ? "DELETE FROM audit_logs WHERE audit_timestamp < {0} AND is_critical_action = true"
                : "DELETE FROM audit_logs WHERE audit_timestamp < {0} AND is_critical_action = false";

            var deletedCount = await _context.Database.ExecuteSqlRawAsync(query, cutoffDate, cancellationToken);
            
            _logger.LogInformation("Medical-grade audit log cleanup completed: {DeletedCount} logs removed, criticalOnly: {CriticalOnly}", 
                deletedCount, criticalActionsOnly);
            
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired medical-grade audit logs");
            return 0;
        }
    }

    public async Task<IEnumerable<AuditLog>> SearchAuditLogsByJsonAsync(string jsonPath, string value, int limit = 100, CancellationToken cancellationToken = default)
    {
        var query = $@"
            SELECT * FROM audit_logs 
            WHERE old_values @> '{{\""" + jsonPath + @"\"": \""" + value + @"\""}}'
               OR new_values @> '{{\""" + jsonPath + @"\"": \""" + value + @"\""}}' 
            ORDER BY audit_timestamp DESC 
            LIMIT {limit}";

        return await _context.AuditLogs
            .FromSqlRaw(query)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}