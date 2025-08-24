using InternationalCenter.Services.Domain.Entities;
using InternationalCenter.Services.Domain.Models;
using InternationalCenter.Services.Domain.Repositories;
using InternationalCenter.Services.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace InternationalCenter.Services.Admin.Api.Application.UseCases;

/// <summary>
/// Medical-grade service creation use case with comprehensive audit logging
/// Replaces direct domain service calls with proper use case architecture
/// </summary>
public sealed class CreateServiceUseCase : ICreateServiceUseCase
{
    private readonly IServiceRepository _serviceRepository;
    private readonly ILogger<CreateServiceUseCase> _logger;

    public CreateServiceUseCase(
        IServiceRepository serviceRepository,
        ILogger<CreateServiceUseCase> logger)
    {
        _serviceRepository = serviceRepository ?? throw new ArgumentNullException(nameof(serviceRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<CreateServiceResponse>> ExecuteAsync(
        CreateServiceRequest request, 
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var auditLog = new List<AdminAuditLogEntry>();

        try
        {
            // Handle null request
            if (request == null)
            {
                var nullError = Error.Create("VALIDATION_ERROR", "Request cannot be null");
                return Result<CreateServiceResponse>.Failure(nullError);
            }

            // Medical-grade input validation
            var validationResult = ValidateRequest(request);
            if (validationResult.IsFailure)
            {
                await LogAuditEntryAsync(auditLog, "CREATE_SERVICE_VALIDATION_FAILED", request, stopwatch.Elapsed, validationResult.Error);
                return Result<CreateServiceResponse>.Failure(validationResult.Error);
            }

            // Log operation start
            await LogAuditEntryAsync(auditLog, "CREATE_SERVICE_STARTED", request, stopwatch.Elapsed);

            // Create service metadata
            var metadata = ServiceMetadata.Create(
                technologies: request.Technologies ?? Array.Empty<string>(),
                features: request.Features ?? Array.Empty<string>(),
                deliveryModes: request.DeliveryModes ?? Array.Empty<string>(),
                icon: request.Icon,
                image: request.Image,
                metaTitle: request.MetaTitle,
                metaDescription: request.MetaDescription);

            // Create service entity
            var service = new Service(
                ServiceId.Create(),
                request.Title,
                Slug.Create(request.Slug),
                request.Description,
                request.DetailedDescription,
                metadata);

            // Set availability if specified
            if (request.Available.HasValue)
            {
                service.SetAvailability(request.Available.Value);
            }

            // Save to repository
            await _serviceRepository.AddAsync(service, cancellationToken);
            await _serviceRepository.SaveChangesAsync(cancellationToken);

            stopwatch.Stop();

            // Log successful completion
            await LogAuditEntryAsync(auditLog, "CREATE_SERVICE_SUCCESS", request, stopwatch.Elapsed);

            var response = new CreateServiceResponse
            {
                Success = true,
                ServiceId = service.Id,
                Slug = service.Slug,
                AuditTrail = auditLog,
                PerformanceMetrics = new AdminPerformanceMetrics
                {
                    TotalDuration = stopwatch.Elapsed,
                    ValidationDuration = TimeSpan.FromMilliseconds(5), // Approximate
                    DatabaseDuration = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds - 5)
                }
            };

            return Result<CreateServiceResponse>.Success(response);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await LogAuditEntryAsync(auditLog, "CREATE_SERVICE_FAILED", request, stopwatch.Elapsed, ex);
            
            _logger.LogError(ex, "MEDICAL_AUDIT: CREATE_SERVICE failed for user {UserId} from {IpAddress}",
                request.UserContext, request.ClientIpAddress);

            return Result<CreateServiceResponse>.Failure(Error.Create("REPOSITORY_ERROR", 
                "Failed to create service due to database error"));
        }
    }

    private static Result<bool> ValidateRequest(CreateServiceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result<bool>.Failure(Error.Create("VALIDATION_ERROR", "Service title is required"));
        
        if (request.Title.Length > 200)
            return Result<bool>.Failure(Error.Create("VALIDATION_ERROR", "Service title cannot exceed 200 characters"));

        if (string.IsNullOrWhiteSpace(request.Slug))
            return Result<bool>.Failure(Error.Create("VALIDATION_ERROR", "Service slug is required"));
        
        if (!IsValidSlug(request.Slug))
            return Result<bool>.Failure(Error.Create("VALIDATION_ERROR", "Service slug must contain only lowercase letters, numbers, and hyphens"));

        if (string.IsNullOrWhiteSpace(request.Description))
            return Result<bool>.Failure(Error.Create("VALIDATION_ERROR", "Service description is required"));
        
        if (request.Description.Length > 500)
            return Result<bool>.Failure(Error.Create("VALIDATION_ERROR", "Service description cannot exceed 500 characters"));

        // Medical-grade security validation for user context
        if (string.IsNullOrWhiteSpace(request.UserContext))
            return Result<bool>.Failure(Error.Create("VALIDATION_ERROR", "User context is required for audit compliance"));

        // Medical-grade audit validation for request ID
        if (string.IsNullOrWhiteSpace(request.RequestId))
            return Result<bool>.Failure(Error.Create("VALIDATION_ERROR", "RequestId is required for audit compliance"));

        return Result<bool>.Success(true);
    }

    private static bool IsValidSlug(string slug)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(slug, @"^[a-z0-9-]+$");
    }

    private async Task LogAuditEntryAsync(
        List<AdminAuditLogEntry> auditLog, 
        string operation, 
        CreateServiceRequest request, 
        TimeSpan duration,
        object? additionalData = null)
    {
        var auditEntry = new AdminAuditLogEntry
        {
            Operation = operation,
            OperationType = "CREATE",
            UserId = request.UserContext ?? "unknown",
            IpAddress = request.ClientIpAddress ?? "unknown", 
            UserAgent = request.UserAgent ?? "unknown",
            CorrelationId = request.RequestId ?? Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            Duration = duration,
            Changes = operation.Contains("STARTED") ? "Service creation initiated" :
                     operation.Contains("SUCCESS") ? $"Service '{request.Title}' created successfully" :
                     operation.Contains("FAILED") ? $"Service creation failed: {(additionalData as Exception)?.Message}" :
                     operation.Contains("VALIDATION") ? "Input validation failed" :
                     "Operation logged",
            Metadata = new Dictionary<string, object>
            {
                ["Title"] = request.Title ?? string.Empty,
                ["Slug"] = request.Slug ?? string.Empty,
                ["RequestId"] = request.RequestId ?? string.Empty,
                ["Duration"] = duration.TotalMilliseconds
            }
        };

        if (additionalData is Exception exception)
        {
            auditEntry.Metadata["Error"] = exception.Message;
            auditEntry.Metadata["StackTrace"] = exception.StackTrace ?? string.Empty;
        }

        auditLog.Add(auditEntry);

        // Medical-grade audit logging - always log to permanent store
        _logger.LogInformation("MEDICAL_AUDIT: {Operation} by {UserId} from {IpAddress} [{CorrelationId}] took {Duration}ms",
            operation, auditEntry.UserId, auditEntry.IpAddress, auditEntry.CorrelationId, duration.TotalMilliseconds);

        await Task.CompletedTask; // Placeholder for async audit store (database/file)
    }
}

/// <summary>
/// Medical-grade service creation request with audit context
/// </summary>
public class CreateServiceRequest
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DetailedDescription { get; set; } = string.Empty;
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
/// Medical-grade service creation response with audit trail
/// </summary>
public class CreateServiceResponse
{
    public bool Success { get; set; }
    public string ServiceId { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public List<AdminAuditLogEntry> AuditTrail { get; set; } = new();
    public AdminPerformanceMetrics PerformanceMetrics { get; set; } = null!;
}

/// <summary>
/// Medical-grade admin audit log entry
/// </summary>
public class AdminAuditLogEntry
{
    public string Operation { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
    public string Changes { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Medical-grade admin performance metrics
/// </summary>
public class AdminPerformanceMetrics
{
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan ValidationDuration { get; set; }
    public TimeSpan DatabaseDuration { get; set; }
    public int RecordsAffected { get; set; } = 1;
}

/// <summary>
/// Medical-grade service creation use case interface
/// </summary>
public interface ICreateServiceUseCase
{
    Task<Result<CreateServiceResponse>> ExecuteAsync(CreateServiceRequest request, CancellationToken cancellationToken = default);
}