using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Service.Audit.Services;

public sealed class AuditService : IAuditService
{
    private readonly IAuditRepository _repository;
    private readonly IAuditSigningService _signingService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditService> _logger;
    private readonly AuditServiceOptions _options;

    public AuditService(
        IAuditRepository repository,
        IAuditSigningService signingService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditService> logger,
        IOptions<AuditServiceOptions> options)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _signingService = signingService ?? throw new ArgumentNullException(nameof(signingService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<string> LogAsync(AuditEventType eventType, string entityType, string entityId, 
        object? oldValues, object? newValues, string? reason = null, 
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Audit logging is disabled, skipping audit event");
            return string.Empty;
        }

        if (eventType == AuditEventType.Read && !_options.LogReadOperations)
        {
            _logger.LogDebug("Read operation auditing is disabled, skipping audit event");
            return string.Empty;
        }

        var auditEvent = await CreateAuditEventAsync(eventType, entityType, entityId, oldValues, newValues, reason, cancellationToken);
        
        var auditId = await _repository.CreateAuditEventAsync(auditEvent, cancellationToken);
        
        _logger.LogInformation("Audit event {AuditId} created for {EventType} on {EntityType}/{EntityId}", 
            auditId, eventType, entityType, entityId);
            
        return auditId;
    }

    public async Task<string> LogAsync(AuditEventType eventType, string entityType, string entityId, 
        string? reason = null, CancellationToken cancellationToken = default)
    {
        return await LogAsync(eventType, entityType, entityId, null, null, reason, cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEvent>> GetAuditTrailAsync(string entityType, string entityId, 
        CancellationToken cancellationToken = default)
    {
        var events = await _repository.GetAuditTrailAsync(entityType, entityId, cancellationToken);
        
        _logger.LogDebug("Retrieved {Count} audit events for {EntityType}/{EntityId}", 
            events.Count, entityType, entityId);
            
        return events;
    }

    public async Task<IReadOnlyList<AuditEvent>> GetAuditTrailAsync(string entityType, string entityId, 
        DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        var events = await _repository.GetAuditTrailAsync(entityType, entityId, from, to, cancellationToken);
        
        _logger.LogDebug("Retrieved {Count} audit events for {EntityType}/{EntityId} from {From} to {To}", 
            events.Count, entityType, entityId, from, to);
            
        return events;
    }

    public async Task<bool> VerifyIntegrityAsync(string auditId, CancellationToken cancellationToken = default)
    {
        var auditEvent = await _repository.GetAuditEventAsync(auditId, cancellationToken);
        
        if (auditEvent == null)
        {
            _logger.LogWarning("Audit event {AuditId} not found for integrity verification", auditId);
            return false;
        }

        var isValid = await _signingService.VerifySignatureAsync(auditEvent, cancellationToken);
        
        _logger.LogInformation("Integrity verification for audit {AuditId}: {IsValid}", auditId, isValid);
        
        return isValid;
    }

    public async Task<AuditIntegrityReport> VerifyEntityIntegrityAsync(string entityType, string entityId, 
        CancellationToken cancellationToken = default)
    {
        var events = await _repository.GetAuditTrailAsync(entityType, entityId, cancellationToken);
        var violations = new List<AuditIntegrityViolation>();
        var validCount = 0;
        var invalidCount = 0;

        foreach (var auditEvent in events)
        {
            var isValid = await _signingService.VerifySignatureAsync(auditEvent, cancellationToken);
            
            if (isValid)
            {
                validCount++;
            }
            else
            {
                invalidCount++;
                var expectedSignature = await _signingService.GenerateSignatureAsync(auditEvent.GetDataForSigning(), cancellationToken);
                
                violations.Add(new AuditIntegrityViolation
                {
                    AuditId = auditEvent.Id,
                    EventType = auditEvent.EventType,
                    Timestamp = auditEvent.Timestamp,
                    ViolationType = "SignatureMismatch",
                    Description = $"Signature verification failed for audit event {auditEvent.Id}",
                    ExpectedSignature = expectedSignature,
                    ActualSignature = auditEvent.Signature
                });
            }
        }

        var report = new AuditIntegrityReport
        {
            EntityType = entityType,
            EntityId = entityId,
            IsValid = violations.Count == 0,
            TotalEvents = events.Count,
            ValidEvents = validCount,
            InvalidEvents = invalidCount,
            VerifiedAt = DateTimeOffset.UtcNow,
            Violations = violations.AsReadOnly()
        };

        _logger.LogInformation("Entity integrity verification for {EntityType}/{EntityId}: {IsValid} " +
                             "({ValidEvents}/{TotalEvents} valid, {Violations} violations)", 
            entityType, entityId, report.IsValid, validCount, events.Count, violations.Count);

        return report;
    }

    private async Task<AuditEvent> CreateAuditEventAsync(AuditEventType eventType, string entityType, 
        string entityId, object? oldValues, object? newValues, string? reason, 
        CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;
        var auditId = Guid.NewGuid().ToString("N");
        
        var auditEvent = new AuditEvent
        {
            Id = auditId,
            EventType = eventType,
            EntityType = entityType,
            EntityId = entityId,
            UserId = context?.User?.Identity?.Name,
            UserName = context?.User?.Identity?.Name,
            SessionId = context?.Session?.Id,
            IpAddress = context?.Connection?.RemoteIpAddress?.ToString(),
            UserAgent = context?.Request?.Headers?.UserAgent.ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            Reason = reason,
            OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            SignatureAlgorithm = _options.SigningAlgorithm,
            CorrelationId = Activity.Current?.Id ?? context?.TraceIdentifier
        };

        var dataToSign = auditEvent.GetDataForSigning();
        var signature = await _signingService.GenerateSignatureAsync(dataToSign, cancellationToken);
        
        return auditEvent with { Signature = signature };
    }
}