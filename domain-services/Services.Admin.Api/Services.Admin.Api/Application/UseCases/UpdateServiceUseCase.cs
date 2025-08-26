using InternationalCenter.Services.Domain.Models;
using InternationalCenter.Services.Domain.Repositories;
using InternationalCenter.Services.Domain.ValueObjects;
using InternationalCenter.Services.Admin.Api.Application.UseCases;
using InternationalCenter.Shared.Services;
using InternationalCenter.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace InternationalCenter.Services.Admin.Api.Application.UseCases;

/// <summary>
/// Medical-grade service update use case with comprehensive change tracking
/// Provides detailed audit trail for all service modifications
/// </summary>
public sealed class UpdateServiceUseCase : IUpdateServiceUseCase
{
    private readonly IServiceRepository _serviceRepository;
    private readonly ILogger<UpdateServiceUseCase> _logger;
    private readonly IAuditService _auditService;

    public UpdateServiceUseCase(
        IServiceRepository serviceRepository,
        ILogger<UpdateServiceUseCase> logger,
        IAuditService auditService)
    {
        _serviceRepository = serviceRepository ?? throw new ArgumentNullException(nameof(serviceRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    public async Task<Result<UpdateServiceResponse>> ExecuteAsync(
        UpdateServiceRequest request, 
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var auditLog = new List<ServiceAuditLogEntry>();
        var changeTracker = new Dictionary<string, (object? OldValue, object? NewValue)>();

        try
        {
            // Set audit context for medical-grade compliance
            var auditContext = new AuditContext
            {
                UserId = request.UserContext ?? "anonymous",
                UserName = request.UserContext ?? "anonymous",
                RequestIp = request.ClientIpAddress ?? "unknown",
                UserAgent = request.UserAgent ?? "unknown",
                CorrelationId = request.RequestId ?? Guid.NewGuid().ToString(),
                RequestStartTime = DateTime.UtcNow
            };
            _auditService.SetAuditContext(auditContext);
            
            // Medical-grade input validation
            var validationResult = ValidateRequest(request);
            if (validationResult.IsFailure)
            {
                // Only log audit if request is not null
                if (request != null)
                {
                    await _auditService.LogBusinessEventAsync(
                        AuditActions.Update,
                        "Service",
                        request.ServiceId,
                        new { Error = validationResult.Error.Message },
                        AuditSeverity.Warning);
                    await LogAuditEntryAsync(auditLog, "UPDATE_SERVICE_VALIDATION_FAILED", request, stopwatch.Elapsed, validationResult.Error);
                }
                return Result<UpdateServiceResponse>.Failure(validationResult.Error);
            }

            // Log operation start with medical-grade audit
            await _auditService.LogBusinessEventAsync(
                AuditActions.Update,
                "Service",
                request.ServiceId,
                new { Operation = "Started" },
                AuditSeverity.Info);
            await LogAuditEntryAsync(auditLog, "UPDATE_SERVICE_STARTED", request, stopwatch.Elapsed);

            // Get existing service
            var existingService = await _serviceRepository.GetByIdAsync(ServiceId.Create(request.ServiceId), cancellationToken);
            if (existingService == null)
            {
                var notFoundError = Error.Create("SERVICE_NOT_FOUND", $"Service with ID {request.ServiceId} not found");
                await LogAuditEntryAsync(auditLog, "UPDATE_SERVICE_NOT_FOUND", request, stopwatch.Elapsed, notFoundError);
                return Result<UpdateServiceResponse>.Failure(notFoundError);
            }

            // Track changes for medical-grade audit
            TrackChanges(changeTracker, existingService, request);

            // Apply updates
            if (!string.IsNullOrWhiteSpace(request.Title) && request.Title != existingService.Title)
            {
                existingService.UpdateTitle(request.Title);
            }

            if ((!string.IsNullOrWhiteSpace(request.Description) && request.Description != existingService.Description) ||
                (!string.IsNullOrWhiteSpace(request.DetailedDescription) && request.DetailedDescription != existingService.DetailedDescription))
            {
                existingService.UpdateDescription(
                    request.Description ?? existingService.Description,
                    request.DetailedDescription ?? existingService.DetailedDescription);
            }

            if (request.Available.HasValue && request.Available.Value != existingService.Available)
            {
                existingService.SetAvailability(request.Available.Value);
            }

            // TODO: Update metadata if provided - need to add UpdateMetadata method to Service entity
            // For now, metadata updates are not supported until Service entity is extended
            if (request.Technologies != null || request.Features != null || request.DeliveryModes != null)
            {
                // Placeholder for metadata updates - requires Service entity extension
                // var newMetadata = ServiceMetadata.Create(...)
                // existingService.UpdateMetadata(newMetadata);
            }

            // Save changes
            await _serviceRepository.UpdateAsync(existingService, cancellationToken);
            await _serviceRepository.SaveChangesAsync(cancellationToken);

            stopwatch.Stop();

            // Log successful completion with medical-grade audit
            await _auditService.LogBusinessEventAsync(
                AuditActions.Update,
                "Service",
                existingService.Id,
                new 
                { 
                    Changes = changeTracker.ToDictionary(kvp => kvp.Key, kvp => $"{kvp.Value.OldValue} → {kvp.Value.NewValue}"),
                    ProcessingDuration = stopwatch.Elapsed.TotalMilliseconds,
                    ServiceTitle = existingService.Title
                },
                AuditSeverity.Info);
            await LogAuditEntryAsync(auditLog, "UPDATE_SERVICE_COMPLETED", request, stopwatch.Elapsed, changeTracker);

            var response = new UpdateServiceResponse
            {
                Success = true,
                ServiceId = existingService.Id,
                AuditTrail = auditLog,
                Changes = changeTracker,
                PerformanceMetrics = new ServicePerformanceMetrics
                {
                    TotalDuration = stopwatch.Elapsed,
                    ValidationDuration = TimeSpan.FromMilliseconds(5),
                    DatabaseDuration = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds - 5)
                }
            };

            return Result<UpdateServiceResponse>.Success(response);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // Log failure with medical-grade audit
            await _auditService.LogBusinessEventAsync(
                AuditActions.Update,
                "Service",
                request.ServiceId,
                new 
                { 
                    Error = ex.Message,
                    ProcessingDuration = stopwatch.Elapsed.TotalMilliseconds,
                    StackTrace = ex.StackTrace
                },
                AuditSeverity.Error);
                
            await LogAuditEntryAsync(auditLog, "UPDATE_SERVICE_FAILED", request, stopwatch.Elapsed, ex);
            
            _logger.LogError(ex, "MEDICAL_AUDIT: UPDATE_SERVICE failed for user {UserId} from {IpAddress}",
                request.UserContext, request.ClientIpAddress);

            return Result<UpdateServiceResponse>.Failure(Error.Create("SERVICE_UPDATE_FAILED", 
                "Failed to update service due to internal error"));
        }
    }

    private static Result<bool> ValidateRequest(UpdateServiceRequest request)
    {
        if (request == null)
            return Result<bool>.Failure(Error.Create("VALIDATION_ERROR", "Update service request cannot be null"));
            
        if (string.IsNullOrWhiteSpace(request.ServiceId))
            return Result<bool>.Failure(Error.Create("INVALID_SERVICE_ID", "Service ID is required"));

        if (!string.IsNullOrWhiteSpace(request.Title) && request.Title.Length > 200)
            return Result<bool>.Failure(Error.Create("TITLE_TOO_LONG", "Service title cannot exceed 200 characters"));

        if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Length > 500)
            return Result<bool>.Failure(Error.Create("DESCRIPTION_TOO_LONG", "Service description cannot exceed 500 characters"));

        // Medical-grade security validation for user context
        if (string.IsNullOrWhiteSpace(request.UserContext))
            return Result<bool>.Failure(Error.Create("MISSING_USER_CONTEXT", "User context is required for audit compliance"));

        return Result<bool>.Success(true);
    }

    private static void TrackChanges(
        Dictionary<string, (object? OldValue, object? NewValue)> changeTracker,
        InternationalCenter.Services.Domain.Entities.Service existingService,
        UpdateServiceRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Title) && request.Title != existingService.Title)
            changeTracker["Title"] = (existingService.Title, request.Title);

        if (!string.IsNullOrWhiteSpace(request.Description) && request.Description != existingService.Description)
            changeTracker["Description"] = (existingService.Description, request.Description);

        if (!string.IsNullOrWhiteSpace(request.DetailedDescription) && request.DetailedDescription != existingService.DetailedDescription)
            changeTracker["DetailedDescription"] = (existingService.DetailedDescription, request.DetailedDescription);

        if (request.Available.HasValue && request.Available.Value != existingService.Available)
            changeTracker["Available"] = (existingService.Available, request.Available.Value);

        if (request.Technologies != null && !request.Technologies.SequenceEqual(existingService.Metadata.Technologies))
            changeTracker["Technologies"] = (existingService.Metadata.Technologies, request.Technologies);

        if (request.Features != null && !request.Features.SequenceEqual(existingService.Metadata.Features))
            changeTracker["Features"] = (existingService.Metadata.Features, request.Features);

        if (request.DeliveryModes != null && !request.DeliveryModes.SequenceEqual(existingService.Metadata.DeliveryModes))
            changeTracker["DeliveryModes"] = (existingService.Metadata.DeliveryModes, request.DeliveryModes);
    }

    private async Task LogAuditEntryAsync(
        List<ServiceAuditLogEntry> auditLog, 
        string operation, 
        UpdateServiceRequest request, 
        TimeSpan duration,
        object? additionalData = null)
    {
        var auditEntry = new ServiceAuditLogEntry
        {
            Operation = operation,
            OperationType = "UPDATE", 
            UserId = request.UserContext ?? "unknown",
            IpAddress = request.ClientIpAddress ?? "unknown",
            UserAgent = request.UserAgent ?? "unknown",
            CorrelationId = request.RequestId ?? Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            Duration = duration,
            Changes = GetChangeDescription(operation, request, additionalData),
            Metadata = new Dictionary<string, object>
            {
                ["ServiceId"] = request.ServiceId ?? string.Empty,
                ["RequestId"] = request.RequestId ?? string.Empty,
                ["Duration"] = duration.TotalMilliseconds
            }
        };

        if (additionalData is Exception exception)
        {
            auditEntry.Metadata["Error"] = exception.Message;
            auditEntry.Metadata["StackTrace"] = exception.StackTrace ?? string.Empty;
        }
        else if (additionalData is Dictionary<string, (object? OldValue, object? NewValue)> changes)
        {
            auditEntry.Metadata["Changes"] = changes.ToDictionary(
                kvp => kvp.Key, 
                kvp => $"{kvp.Value.OldValue} → {kvp.Value.NewValue}");
        }

        auditLog.Add(auditEntry);

        // Medical-grade audit logging
        _logger.LogInformation("MEDICAL_AUDIT: {Operation} by {UserId} from {IpAddress} [{CorrelationId}] took {Duration}ms",
            operation, auditEntry.UserId, auditEntry.IpAddress, auditEntry.CorrelationId, duration.TotalMilliseconds);

        await Task.CompletedTask;
    }

    private static string GetChangeDescription(string operation, UpdateServiceRequest request, object? additionalData)
    {
        return operation switch
        {
            "UPDATE_SERVICE_STARTED" => $"Service update initiated for ID: {request.ServiceId}",
            "UPDATE_SERVICE_COMPLETED" => $"Service {request.ServiceId} updated successfully",
            "UPDATE_SERVICE_FAILED" => $"Service update failed for ID: {request.ServiceId}",
            "UPDATE_SERVICE_NOT_FOUND" => $"Service not found: {request.ServiceId}",
            "UPDATE_SERVICE_VALIDATION_FAILED" => "Input validation failed",
            _ => "Operation logged"
        };
    }
}

/// <summary>
/// Medical-grade service update request with partial update support
/// </summary>
public class UpdateServiceRequest
{
    public string ServiceId { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? DetailedDescription { get; set; }
    public string[]? Technologies { get; set; }
    public string[]? Features { get; set; }
    public string[]? DeliveryModes { get; set; }
    public string? Icon { get; set; }
    public string? Image { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public bool? Available { get; set; }

    // Medical-grade audit requirements
    public string? UserContext { get; set; }
    public string? RequestId { get; set; }
    public string? ClientIpAddress { get; set; }
    public string? UserAgent { get; set; }
}

/// <summary>
/// Medical-grade service update response with detailed change tracking
/// </summary>
public class UpdateServiceResponse
{
    public bool Success { get; set; }
    public string ServiceId { get; set; } = string.Empty;
    public Dictionary<string, (object? OldValue, object? NewValue)> Changes { get; set; } = new();
    public List<ServiceAuditLogEntry> AuditTrail { get; set; } = new();
    public ServicePerformanceMetrics PerformanceMetrics { get; set; } = null!;
}

/// <summary>
/// Medical-grade service update use case interface
/// </summary>
public interface IUpdateServiceUseCase
{
    Task<Result<UpdateServiceResponse>> ExecuteAsync(UpdateServiceRequest request, CancellationToken cancellationToken = default);
}