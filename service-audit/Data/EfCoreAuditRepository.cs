using Microsoft.EntityFrameworkCore;

namespace Service.Audit.Data;

public sealed class EfCoreAuditRepository : IAuditRepository
{
    private readonly AuditDbContext _context;
    private readonly ILogger<EfCoreAuditRepository> _logger;

    public EfCoreAuditRepository(AuditDbContext context, ILogger<EfCoreAuditRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> CreateAuditEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        if (auditEvent == null)
        {
            throw new ArgumentNullException(nameof(auditEvent));
        }

        try
        {
            _context.AuditEvents.Add(auditEvent);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Created audit event {AuditId} for {EventType} on {EntityType}/{EntityId}",
                auditEvent.Id, auditEvent.EventType, auditEvent.EntityType, auditEvent.EntityId);

            return auditEvent.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit event {AuditId}", auditEvent.Id);
            throw;
        }
    }

    public async Task<AuditEvent?> GetAuditEventAsync(string auditId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(auditId))
        {
            throw new ArgumentException("Audit ID cannot be null or empty", nameof(auditId));
        }

        try
        {
            var auditEvent = await _context.AuditEvents
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == auditId, cancellationToken);

            _logger.LogDebug("Retrieved audit event {AuditId}: {Found}", 
                auditId, auditEvent != null ? "found" : "not found");

            return auditEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve audit event {AuditId}", auditId);
            throw;
        }
    }

    public async Task<IReadOnlyList<AuditEvent>> GetAuditTrailAsync(string entityType, string entityId, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(entityType))
        {
            throw new ArgumentException("Entity type cannot be null or empty", nameof(entityType));
        }

        if (string.IsNullOrEmpty(entityId))
        {
            throw new ArgumentException("Entity ID cannot be null or empty", nameof(entityId));
        }

        try
        {
            var events = await _context.AuditEvents
                .AsNoTracking()
                .Where(e => e.EntityType == entityType && e.EntityId == entityId)
                .OrderBy(e => e.Timestamp)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Retrieved {Count} audit events for {EntityType}/{EntityId}", 
                events.Count, entityType, entityId);

            return events.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve audit trail for {EntityType}/{EntityId}", 
                entityType, entityId);
            throw;
        }
    }

    public async Task<IReadOnlyList<AuditEvent>> GetAuditTrailAsync(string entityType, string entityId, 
        DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(entityType))
        {
            throw new ArgumentException("Entity type cannot be null or empty", nameof(entityType));
        }

        if (string.IsNullOrEmpty(entityId))
        {
            throw new ArgumentException("Entity ID cannot be null or empty", nameof(entityId));
        }

        if (from > to)
        {
            throw new ArgumentException("From date cannot be greater than to date");
        }

        try
        {
            var events = await _context.AuditEvents
                .AsNoTracking()
                .Where(e => e.EntityType == entityType && e.EntityId == entityId)
                .Where(e => e.Timestamp >= from && e.Timestamp <= to)
                .OrderBy(e => e.Timestamp)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Retrieved {Count} audit events for {EntityType}/{EntityId} from {From} to {To}", 
                events.Count, entityType, entityId, from, to);

            return events.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve audit trail for {EntityType}/{EntityId} from {From} to {To}", 
                entityType, entityId, from, to);
            throw;
        }
    }

    public async Task<IReadOnlyList<AuditEvent>> GetAuditEventsAsync(DateTimeOffset from, DateTimeOffset to, 
        CancellationToken cancellationToken = default)
    {
        if (from > to)
        {
            throw new ArgumentException("From date cannot be greater than to date");
        }

        try
        {
            var events = await _context.AuditEvents
                .AsNoTracking()
                .Where(e => e.Timestamp >= from && e.Timestamp <= to)
                .OrderBy(e => e.Timestamp)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Retrieved {Count} audit events from {From} to {To}", 
                events.Count, from, to);

            return events.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve audit events from {From} to {To}", from, to);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string auditId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(auditId))
        {
            throw new ArgumentException("Audit ID cannot be null or empty", nameof(auditId));
        }

        try
        {
            var exists = await _context.AuditEvents
                .AsNoTracking()
                .AnyAsync(e => e.Id == auditId, cancellationToken);

            _logger.LogDebug("Audit event {AuditId} exists: {Exists}", auditId, exists);

            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if audit event {AuditId} exists", auditId);
            throw;
        }
    }
}