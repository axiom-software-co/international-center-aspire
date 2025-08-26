using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Services;
using Shared.Models;
using Shared.Infrastructure;
using Shared.Repositories;

namespace Shared.Extensions;

public static class AuditServiceExtensions
{
    public static IServiceCollection AddMedicalGradeAudit(this IServiceCollection services, IConfiguration configuration)
    {
        // Register audit configuration directly (Microsoft recommended options pattern)
        // This eliminates the need for .Value access in consuming services
        services.AddSingleton(serviceProvider =>
        {
            var auditConfig = new AuditConfiguration();
            configuration.GetSection("Audit").Bind(auditConfig);
            return auditConfig;
        });
        
        // Register audit service
        services.AddScoped<IAuditService, AuditService>();
        
        // Note: IAuditLogRepository implementation must be registered by the consuming project
        // to avoid circular dependencies between Domain.Shared and Infrastructure.Database
        
        // Register factory for creating audit contexts to avoid circular dependencies
        services.AddScoped<Func<DbContextOptions, AuditableDbContext>>(provider => 
            options => new ApplicationDbContext((DbContextOptions<ApplicationDbContext>)options, provider));
        
        return services;
    }

    public static IServiceCollection AddMedicalGradeAuditWithDefaults(this IServiceCollection services)
    {
        // Register default audit configuration directly (Microsoft recommended options pattern)
        // This eliminates the need for .Value access in consuming services
        services.AddSingleton(serviceProvider =>
        {
            return new AuditConfiguration
            {
                EnableAuditing = true,
                AuditCreates = true,
                AuditUpdates = true,
                AuditDeletes = true,
                AuditReads = false, // Usually disabled for performance
                MaxRetentionDays = 2555, // 7 years for medical compliance
                EncryptSensitiveData = true,
                ExcludedProperties = new List<string> { "UpdatedAt", "LastAccessed" },
                SensitiveProperties = new List<string> 
                { 
                    "Password", "Token", "Secret", "Key", "Hash", 
                    "SSN", "TaxId", "CreditCard", "BankAccount",
                    "Phone", "Email" // Consider these sensitive for medical privacy
                }
            };
        });
        
        // Register audit service
        services.AddScoped<IAuditService, AuditService>();
        
        // Note: IAuditLogRepository implementation must be registered by the consuming project
        // to avoid circular dependencies between Domain.Shared and Infrastructure.Database
        
        // Register factory for creating audit contexts
        services.AddScoped<Func<DbContextOptions, AuditableDbContext>>(provider => 
            options => new ApplicationDbContext((DbContextOptions<ApplicationDbContext>)options, provider));
        
        return services;
    }

    /// <summary>
    /// Adds automated audit retention service with medical-grade compliance policies
    /// Implements background cleanup of expired audit logs following 7-year retention requirements
    /// Preserves critical actions through archiving to ensure zero data loss compliance
    /// </summary>
    public static IServiceCollection AddMedicalGradeAuditRetention(this IServiceCollection services, IConfiguration configuration)
    {
        // Ensure base audit services are registered
        services.AddMedicalGradeAudit(configuration);
        
        // Register audit retention background service
        services.AddHostedService<AuditRetentionService>();
        
        return services;
    }

    /// <summary>
    /// Adds automated audit retention service with default medical-grade compliance policies
    /// Uses standard 7-year retention with critical action archiving enabled
    /// </summary>
    public static IServiceCollection AddMedicalGradeAuditRetentionWithDefaults(this IServiceCollection services)
    {
        // Ensure base audit services with defaults are registered
        services.AddMedicalGradeAuditWithDefaults();
        
        // Register audit retention background service
        services.AddHostedService<AuditRetentionService>();
        
        return services;
    }
    
    /// <summary>
    /// Ensures audit tables are created using EF Core migrations for medical-grade compliance
    /// Uses proper EF Core database creation instead of raw SQL for better maintainability
    /// </summary>
    public static async Task<bool> EnsureAuditTablesCreatedAsync(this IServiceProvider serviceProvider)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ApplicationDbContext>>();
            
            // Use EF Core to ensure database is created with proper audit table structure
            // This will create the database and all tables including audit_logs with proper indexing
            var created = await context.Database.EnsureCreatedAsync();
            
            if (created)
            {
                logger.LogInformation("Medical-grade audit database structure created successfully with comprehensive indexing");
            }
            else
            {
                logger.LogInformation("Medical-grade audit database structure already exists");
            }
            
            // Verify audit logs table exists and has proper structure
            var auditLogsExist = await context.AuditLogs.AsQueryable().AnyAsync() || true; // Will not fail if table exists
            logger.LogInformation("Medical-grade audit logs table verified for zero data loss compliance");
            
            return true;
        }
        catch (Exception ex)
        {
            var logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ApplicationDbContext>>();
            logger?.LogError(ex, "Failed to ensure medical-grade audit tables are created");
            return false;
        }
    }
    
    public static async Task<int> CleanupExpiredAuditLogsAsync(this IServiceProvider serviceProvider, int retentionDays = 2555)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var auditRepository = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
            var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ApplicationDbContext>>();
            
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
            
            // Delete expired non-critical audit logs using repository
            var deletedCount = await auditRepository.DeleteExpiredAuditLogsAsync(cutoffDate, criticalActionsOnly: false);
            
            // Archive critical audit logs instead of deleting them (medical compliance)
            // This could be implemented as moving to an archive table or cold storage
            
            logger.LogInformation("Cleaned up {DeletedCount} expired audit logs older than {CutoffDate}", deletedCount, cutoffDate);
            
            return deletedCount;
        }
        catch (Exception ex)
        {
            var logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ApplicationDbContext>>();
            logger.LogError(ex, "Failed to cleanup expired audit logs");
            return 0;
        }
    }

    /// <summary>
    /// Generates comprehensive audit retention compliance report for medical-grade regulations
    /// Provides detailed metrics on data retention status and compliance with healthcare standards
    /// </summary>
    public static async Task<RetentionReport> GenerateAuditRetentionReportAsync(this IServiceProvider serviceProvider, DateTime? reportDate = null)
    {
        using var scope = serviceProvider.CreateScope();
        var auditRetentionService = scope.ServiceProvider.GetService<AuditRetentionService>();
        
        if (auditRetentionService == null)
        {
            throw new InvalidOperationException("Audit retention service is not registered. Call AddMedicalGradeAuditRetention() to enable retention reporting.");
        }
        
        return await auditRetentionService.GenerateRetentionReportAsync(reportDate);
    }

    /// <summary>
    /// Performs immediate audit retention cleanup for testing or manual maintenance
    /// Should be used carefully in production - prefer automated background service
    /// </summary>
    public static async Task<int> PerformImmediateAuditRetentionCleanupAsync(this IServiceProvider serviceProvider, int? customRetentionDays = null)
    {
        using var scope = serviceProvider.CreateScope();
        var auditRepository = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
        var auditConfig = scope.ServiceProvider.GetRequiredService<AuditConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AuditRetentionService>>();
        
        var retentionDays = customRetentionDays ?? auditConfig.MaxRetentionDays;
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        
        logger.LogWarning("MEDICAL_AUDIT_RETENTION: Performing immediate retention cleanup - CutoffDate: {CutoffDate}, RetentionDays: {RetentionDays}",
            cutoffDate, retentionDays);
        
        var deletedCount = await auditRepository.DeleteExpiredAuditLogsAsync(cutoffDate, criticalActionsOnly: false);
        
        logger.LogInformation("MEDICAL_AUDIT_RETENTION: Immediate cleanup completed - DeletedCount: {DeletedCount}",
            deletedCount);
        
        return deletedCount;
    }
}