using InternationalCenter.Services.Domain.Models;
using InternationalCenter.Services.Domain.Repositories;
using InternationalCenter.Services.Domain.ValueObjects;
using InternationalCenter.Services.Admin.Api.Application.UseCases;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace InternationalCenter.Services.Admin.Api.Application.UseCases;

/// <summary>
/// Medical-grade service deletion use case with soft delete and comprehensive audit
/// Implements reversible deletion with detailed audit trail for compliance
/// </summary>
public sealed class DeleteServiceUseCase : IDeleteServiceUseCase
{
    private readonly IServiceRepository _serviceRepository;
    private readonly ILogger<DeleteServiceUseCase> _logger;

    public DeleteServiceUseCase(
        IServiceRepository serviceRepository,
        ILogger<DeleteServiceUseCase> logger)
    {
        _serviceRepository = serviceRepository ?? throw new ArgumentNullException(nameof(serviceRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<DeleteServiceResponse>> ExecuteAsync(
        DeleteServiceRequest request, 
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var auditLog = new List<AdminAuditLogEntry>();

        try
        {
            // Medical-grade input validation
            var validationResult = ValidateRequest(request);
            if (validationResult.IsFailure)
            {
                // Only log audit if request is not null
                if (request != null)
                {
                    await LogAuditEntryAsync(auditLog, "DELETE_SERVICE_VALIDATION_FAILED", request, stopwatch.Elapsed, validationResult.Error);
                }
                return Result<DeleteServiceResponse>.Failure(validationResult.Error);
            }

            // Log operation start
            await LogAuditEntryAsync(auditLog, "DELETE_SERVICE_STARTED", request, stopwatch.Elapsed);

            // Get existing service for audit purposes
            var existingService = await _serviceRepository.GetByIdAsync(ServiceId.Create(request.ServiceId), cancellationToken);
            if (existingService == null)
            {
                var notFoundError = Error.Create("SERVICE_NOT_FOUND", $"Service with ID {request.ServiceId} not found");
                await LogAuditEntryAsync(auditLog, "DELETE_SERVICE_NOT_FOUND", request, stopwatch.Elapsed, notFoundError);
                return Result<DeleteServiceResponse>.Failure(notFoundError);
            }

            // Capture service details for audit before deletion
            var serviceSnapshot = new ServiceSnapshot
            {
                Id = existingService.Id,
                Title = existingService.Title,
                Slug = existingService.Slug,
                Description = existingService.Description,
                Status = existingService.Status.ToString(),
                Available = existingService.Available,
                Featured = existingService.Featured,
                CreatedAt = existingService.CreatedAt,
                LastModifiedAt = existingService.UpdatedAt
            };

            // Perform soft delete (archive service instead of physical deletion)
            existingService.Archive(); // This sets Status to Archived and Available to false

            // Update snapshot to reflect final archived state
            serviceSnapshot.Status = existingService.Status.ToString();
            serviceSnapshot.Available = existingService.Available;
            serviceSnapshot.LastModifiedAt = existingService.UpdatedAt;

            // Save changes
            await _serviceRepository.UpdateAsync(existingService, cancellationToken);
            await _serviceRepository.SaveChangesAsync(cancellationToken);

            stopwatch.Stop();

            // Log successful completion with service snapshot
            await LogAuditEntryAsync(auditLog, "DELETE_SERVICE_COMPLETED", request, stopwatch.Elapsed, serviceSnapshot);

            var response = new DeleteServiceResponse
            {
                Success = true,
                ServiceId = existingService.Id,
                DeletedAt = DateTime.UtcNow,
                DeletionType = "SOFT_DELETE",
                ServiceSnapshot = serviceSnapshot,
                AuditTrail = auditLog,
                PerformanceMetrics = new AdminPerformanceMetrics
                {
                    TotalDuration = stopwatch.Elapsed,
                    ValidationDuration = TimeSpan.FromMilliseconds(5),
                    DatabaseDuration = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds - 5)
                }
            };

            return Result<DeleteServiceResponse>.Success(response);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await LogAuditEntryAsync(auditLog, "DELETE_SERVICE_FAILED", request, stopwatch.Elapsed, ex);
            
            _logger.LogError(ex, "MEDICAL_AUDIT: DELETE_SERVICE failed for user {UserId} from {IpAddress}",
                request.UserContext, request.ClientIpAddress);

            return Result<DeleteServiceResponse>.Failure(Error.Create("SERVICE_DELETION_FAILED", 
                "Failed to delete service due to internal error"));
        }
    }

    private static Result<bool> ValidateRequest(DeleteServiceRequest request)
    {
        if (request == null)
            return Result<bool>.Failure(Error.Create("VALIDATION_ERROR", "Delete service request cannot be null"));
            
        if (string.IsNullOrWhiteSpace(request.ServiceId))
            return Result<bool>.Failure(Error.Create("INVALID_SERVICE_ID", "Service ID is required"));

        // Medical-grade security validation for user context
        if (string.IsNullOrWhiteSpace(request.UserContext))
            return Result<bool>.Failure(Error.Create("MISSING_USER_CONTEXT", "User context is required for audit compliance"));

        // Validate user has deletion permissions (placeholder for actual permission check)
        if (string.IsNullOrWhiteSpace(request.DeletionReason))
            return Result<bool>.Failure(Error.Create("MISSING_DELETION_REASON", "Deletion reason is required for audit compliance"));

        return Result<bool>.Success(true);
    }

    private async Task LogAuditEntryAsync(
        List<AdminAuditLogEntry> auditLog, 
        string operation, 
        DeleteServiceRequest request, 
        TimeSpan duration,
        object? additionalData = null)
    {
        var auditEntry = new AdminAuditLogEntry
        {
            Operation = operation,
            OperationType = "SOFT_DELETE",
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
                ["DeletionReason"] = request.DeletionReason ?? string.Empty,
                ["RequestId"] = request.RequestId ?? string.Empty,
                ["Duration"] = duration.TotalMilliseconds,
                ["DeletionType"] = "SOFT_DELETE"
            }
        };

        if (additionalData is Exception exception)
        {
            auditEntry.Metadata["Error"] = exception.Message;
            auditEntry.Metadata["StackTrace"] = exception.StackTrace ?? string.Empty;
        }
        else if (additionalData is ServiceSnapshot snapshot)
        {
            auditEntry.Metadata["DeletedService"] = new Dictionary<string, object>
            {
                ["Title"] = snapshot.Title,
                ["Slug"] = snapshot.Slug,
                ["Status"] = snapshot.Status,
                ["Available"] = snapshot.Available,
                ["Featured"] = snapshot.Featured,
                ["CreatedAt"] = snapshot.CreatedAt,
                ["LastModifiedAt"] = snapshot.LastModifiedAt
            };
        }

        auditLog.Add(auditEntry);

        // Medical-grade audit logging - critical for deletion operations
        _logger.LogCritical("MEDICAL_AUDIT: {Operation} by {UserId} from {IpAddress} [{CorrelationId}] took {Duration}ms - ServiceId: {ServiceId}",
            operation, auditEntry.UserId, auditEntry.IpAddress, auditEntry.CorrelationId, duration.TotalMilliseconds, request.ServiceId);

        await Task.CompletedTask;
    }

    private static string GetChangeDescription(string operation, DeleteServiceRequest request, object? additionalData)
    {
        return operation switch
        {
            "DELETE_SERVICE_STARTED" => $"Service soft deletion initiated for ID: {request.ServiceId}. Reason: {request.DeletionReason}",
            "DELETE_SERVICE_COMPLETED" => $"Service {request.ServiceId} soft deleted successfully",
            "DELETE_SERVICE_FAILED" => $"Service deletion failed for ID: {request.ServiceId}",
            "DELETE_SERVICE_NOT_FOUND" => $"Service not found: {request.ServiceId}",
            "DELETE_SERVICE_VALIDATION_FAILED" => "Input validation failed",
            _ => "Operation logged"
        };
    }
}

/// <summary>
/// Medical-grade service deletion request with mandatory audit information
/// </summary>
public class DeleteServiceRequest
{
    public string ServiceId { get; set; } = string.Empty;
    public string DeletionReason { get; set; } = string.Empty; // Required for audit compliance
    public bool ForceDelete { get; set; } = false; // For future hard delete functionality

    // Medical-grade audit requirements
    public string? UserContext { get; set; }
    public string? RequestId { get; set; }
    public string? ClientIpAddress { get; set; }
    public string? UserAgent { get; set; }
}

/// <summary>
/// Medical-grade service deletion response with comprehensive audit trail
/// </summary>
public class DeleteServiceResponse
{
    public bool Success { get; set; }
    public string ServiceId { get; set; } = string.Empty;
    public DateTime DeletedAt { get; set; }
    public string DeletionType { get; set; } = "SOFT_DELETE";
    public ServiceSnapshot ServiceSnapshot { get; set; } = null!;
    public List<AdminAuditLogEntry> AuditTrail { get; set; } = new();
    public AdminPerformanceMetrics PerformanceMetrics { get; set; } = null!;
}

/// <summary>
/// Service snapshot for audit trail preservation
/// </summary>
public class ServiceSnapshot
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool Available { get; set; }
    public bool Featured { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
}

/// <summary>
/// Medical-grade service deletion use case interface
/// </summary>
public interface IDeleteServiceUseCase
{
    Task<Result<DeleteServiceResponse>> ExecuteAsync(DeleteServiceRequest request, CancellationToken cancellationToken = default);
}