using Shared.Models;

namespace Shared.Repositories;

/// <summary>
/// Medical-grade audit log repository contract for zero data loss compliance
/// Follows domain repository pattern with proper dependency inversion
/// Optimized for PostgreSQL with high-performance query patterns
/// </summary>
public interface IAuditLogRepository
{
    Task<bool> CreateAuditLogAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
    Task<bool> CreateAuditLogsBatchAsync(IEnumerable<AuditLog> auditLogs, CancellationToken cancellationToken = default);
    Task<AuditLog?> GetAuditLogByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetAuditLogsByEntityAsync(string entityType, string entityId, int limit = 100, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetAuditLogsByUserAsync(string userId, int limit = 100, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetAuditLogsByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetCriticalAuditLogsAsync(DateTime fromDate, DateTime toDate, int limit = 100, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetAuditLogsByDateRangeAsync(DateTime fromDate, DateTime toDate, int limit = 100, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetAuditLogsByActionAsync(string action, DateTime fromDate, DateTime toDate, int limit = 100, CancellationToken cancellationToken = default);
    Task<int> GetAuditLogsCountAsync(string? entityType = null, string? action = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<int> DeleteExpiredAuditLogsAsync(DateTime cutoffDate, bool criticalActionsOnly = false, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> SearchAuditLogsByJsonAsync(string jsonPath, string value, int limit = 100, CancellationToken cancellationToken = default);
}