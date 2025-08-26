using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Shared.Models;
using Shared.Services;
using System.Security.Claims;
using System.Diagnostics;

namespace Shared.Infrastructure;

public abstract class AuditableDbContext : BaseDatabaseContext
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditableDbContext> _logger;
    private readonly DbContextOptions _options;

    protected AuditableDbContext(DbContextOptions options, IServiceProvider serviceProvider) : base(options)
    {
        _options = options;
        _serviceProvider = serviceProvider;
        _httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>() ?? throw new InvalidOperationException("IHttpContextAccessor not registered");
        _logger = serviceProvider.GetRequiredService<ILogger<AuditableDbContext>>();
    }

    // Add AuditLogs DbSet for audit storage
    public DbSet<AuditLog> AuditLogs { get; set; }

    public override int SaveChanges()
    {
        return SaveChangesAsync().GetAwaiter().GetResult();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditContext = CreateAuditContext();
        var auditLogs = new List<AuditLog>();

        try
        {
            // Capture audit information before calling base SaveChanges
            using var scope = _serviceProvider.CreateScope();
            var auditService = scope.ServiceProvider.GetService<IAuditService>();
            
            if (auditService != null)
            {
                auditService.SetAuditContext(auditContext);
                auditLogs = await auditService.CaptureChangesAsync(ChangeTracker, auditContext);
            }
        }
        catch (Exception ex)
        {
            // Medical-grade compliance: Never fail the main operation due to audit capture issues
            _logger.LogError(ex, "Failed to capture audit information during SaveChanges");
        }

        // Call base SaveChanges to persist the main entity changes
        var result = await base.SaveChangesAsync(cancellationToken);

        try
        {
            // Save audit logs after successful entity persistence
            if (auditLogs.Any())
            {
                await SaveAuditLogsAsync(auditLogs, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // Medical-grade compliance: Log audit save failures but don't fail the main operation
            _logger.LogError(ex, "Failed to save audit logs after successful entity persistence. {AuditCount} audit entries may be lost", auditLogs.Count);
            
            // For medical-grade compliance, attempt to save critical audit information to logging system
            foreach (var audit in auditLogs.Where(a => a.IsCriticalAction))
            {
                try
                {
                    _logger.LogCritical("AUDIT BACKUP: Critical action {Action} on {EntityType}:{EntityId} by {UserId} at {Timestamp}",
                        audit.Action, audit.EntityType, audit.EntityId, audit.UserId, audit.AuditTimestamp);
                }
                catch
                {
                    // Last resort: at least we tried to preserve critical audit information
                }
            }
        }

        return result;
    }

    private async Task SaveAuditLogsAsync(List<AuditLog> auditLogs, CancellationToken cancellationToken)
    {
        // Create a separate context to avoid change tracking conflicts
        using var auditScope = _serviceProvider.CreateScope();
        var auditContextProvider = auditScope.ServiceProvider.GetRequiredService<Func<DbContextOptions, AuditableDbContext>>();
        
        using var auditContext = auditContextProvider(_options);
        
        auditContext.AuditLogs.AddRange(auditLogs);
        await auditContext.Database.ExecuteSqlRawAsync("SET session_replication_role = replica;", cancellationToken);
        
        try
        {
            await auditContext.SaveChangesWithoutAuditAsync(cancellationToken);
        }
        finally
        {
            await auditContext.Database.ExecuteSqlRawAsync("SET session_replication_role = DEFAULT;", cancellationToken);
        }
    }

    // Internal method to save without triggering audit capturing (prevents infinite recursion)
    internal async Task<int> SaveChangesWithoutAuditAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }

    private AuditContext CreateAuditContext()
    {
        var context = new AuditContext
        {
            CorrelationId = Activity.Current?.Id ?? Guid.NewGuid().ToString(),
            TraceId = Activity.Current?.TraceId.ToString() ?? "",
            RequestStartTime = DateTime.UtcNow
        };

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            // HTTP context information
            context.RequestUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{httpContext.Request.Path}{httpContext.Request.QueryString}";
            context.RequestMethod = httpContext.Request.Method;
            context.RequestIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            context.UserAgent = httpContext.Request.Headers.UserAgent.FirstOrDefault()?.Substring(0, Math.Min(100, httpContext.Request.Headers.UserAgent.FirstOrDefault()?.Length ?? 0)) ?? "unknown";
            
            // Session information
            context.SessionId = httpContext.Session?.Id ?? httpContext.Connection.Id;
            
            // User information
            var user = httpContext.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                context.UserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                                 user.FindFirst("sub")?.Value ??
                                 user.FindFirst("user_id")?.Value ?? "authenticated_unknown";
                context.UserName = user.FindFirst(ClaimTypes.Name)?.Value ?? user.Identity.Name ?? "unknown";
            }
            else
            {
                context.UserId = "anonymous";
                context.UserName = "anonymous";
            }

            // Client application information
            context.ClientApplication = httpContext.Request.Headers["X-Client-Application"].FirstOrDefault();
            
            // Request ID for correlation
            if (httpContext.Request.Headers.ContainsKey("X-Request-ID"))
            {
                context.CorrelationId = httpContext.Request.Headers["X-Request-ID"].FirstOrDefault() ?? context.CorrelationId;
            }
        }
        else
        {
            // Fallback for non-HTTP contexts (background services, etc.)
            context.RequestUrl = "N/A";
            context.RequestMethod = "N/A";
            context.RequestIp = "N/A";
            context.UserAgent = "System Process";
            context.UserId = "system";
            context.UserName = "system";
            context.SessionId = Environment.MachineName + "_" + Environment.ProcessId;
        }

        // Version information
        var versionService = _serviceProvider.GetService<IVersionService>();
        if (versionService != null)
        {
            context.AppVersion = versionService.GetVersion();
            context.BuildDate = versionService.BuildDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }
        else
        {
            context.AppVersion = "unknown";
            context.BuildDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }

        return context;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure AuditLog entity
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            
            // Primary indexes for query performance
            entity.HasIndex(e => new { e.EntityType, e.EntityId })
                  .HasDatabaseName("IX_AuditLogs_Entity");
            
            entity.HasIndex(e => e.UserId)
                  .HasDatabaseName("IX_AuditLogs_UserId");
            
            entity.HasIndex(e => e.AuditTimestamp)
                  .HasDatabaseName("IX_AuditLogs_AuditTimestamp");
            
            entity.HasIndex(e => e.Action)
                  .HasDatabaseName("IX_AuditLogs_Action");
            
            entity.HasIndex(e => e.CorrelationId)
                  .HasDatabaseName("IX_AuditLogs_CorrelationId");
            
            entity.HasIndex(e => e.Severity)
                  .HasDatabaseName("IX_AuditLogs_Severity");
            
            entity.HasIndex(e => e.IsCriticalAction)
                  .HasDatabaseName("IX_AuditLogs_IsCriticalAction")
                  .HasFilter("is_critical_action = true");
            
            // Composite indexes for common query patterns
            entity.HasIndex(e => new { e.EntityType, e.AuditTimestamp })
                  .HasDatabaseName("IX_AuditLogs_EntityType_AuditTimestamp");
            
            entity.HasIndex(e => new { e.UserId, e.AuditTimestamp })
                  .HasDatabaseName("IX_AuditLogs_UserId_AuditTimestamp");
            
            entity.HasIndex(e => new { e.IsCriticalAction, e.AuditTimestamp })
                  .HasDatabaseName("IX_AuditLogs_IsCriticalAction_AuditTimestamp")
                  .HasFilter("is_critical_action = true");
            
            // Configure JSONB columns for PostgreSQL
            entity.Property(e => e.OldValues).HasColumnType("jsonb");
            entity.Property(e => e.NewValues).HasColumnType("jsonb");
            entity.Property(e => e.ChangedProperties).HasColumnType("jsonb");
        });
    }
}