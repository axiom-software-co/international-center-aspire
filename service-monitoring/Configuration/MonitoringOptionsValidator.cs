namespace Service.Monitoring.Configuration;

public sealed class MonitoringOptionsValidator : AbstractValidator<MonitoringOptions>
{
    public MonitoringOptionsValidator()
    {
        RuleFor(x => x.HealthCheckPath)
            .NotEmpty()
            .WithMessage("Health check path is required")
            .Must(BeValidPath)
            .WithMessage("Health check path must start with '/'");
            
        RuleFor(x => x.ReadinessPath)
            .NotEmpty()
            .WithMessage("Readiness path is required")
            .Must(BeValidPath)
            .WithMessage("Readiness path must start with '/'");
            
        RuleFor(x => x.LivenessPath)
            .NotEmpty()
            .WithMessage("Liveness path is required")
            .Must(BeValidPath)
            .WithMessage("Liveness path must start with '/'");
            
        RuleFor(x => x.HealthCheckTimeout)
            .GreaterThanOrEqualTo(TimeSpan.FromSeconds(1))
            .WithMessage("Health check timeout must be at least 1 second")
            .LessThanOrEqualTo(TimeSpan.FromMinutes(5))
            .WithMessage("Health check timeout cannot exceed 5 minutes");
            
        RuleFor(x => x.HealthCheckInterval)
            .GreaterThanOrEqualTo(TimeSpan.FromSeconds(10))
            .WithMessage("Health check interval must be at least 10 seconds")
            .LessThanOrEqualTo(TimeSpan.FromHours(1))
            .WithMessage("Health check interval cannot exceed 1 hour");
            
        RuleFor(x => x.CacheDuration)
            .GreaterThanOrEqualTo(TimeSpan.FromSeconds(1))
            .WithMessage("Cache duration must be at least 1 second")
            .LessThanOrEqualTo(TimeSpan.FromMinutes(10))
            .WithMessage("Cache duration cannot exceed 10 minutes")
            .When(x => x.CacheResults);
        
        RuleFor(x => x.Database)
            .SetValidator(new DatabaseHealthOptionsValidator());
            
        RuleFor(x => x.Redis)
            .SetValidator(new RedisHealthOptionsValidator());
            
        RuleFor(x => x.Metrics)
            .SetValidator(new MetricsOptionsValidator());
    }
    
    private static bool BeValidPath(string path)
    {
        return !string.IsNullOrEmpty(path) && path.StartsWith('/');
    }
}

public sealed class DatabaseHealthOptionsValidator : AbstractValidator<DatabaseHealthOptions>
{
    public DatabaseHealthOptionsValidator()
    {
        RuleFor(x => x.ConnectionString)
            .NotEmpty()
            .When(x => x.Enabled)
            .WithMessage("Database connection string is required when database health checks are enabled");
            
        RuleFor(x => x.Timeout)
            .GreaterThanOrEqualTo(TimeSpan.FromSeconds(1))
            .WithMessage("Database timeout must be at least 1 second")
            .LessThanOrEqualTo(TimeSpan.FromSeconds(30))
            .WithMessage("Database timeout cannot exceed 30 seconds");
            
        RuleFor(x => x.TestQuery)
            .NotEmpty()
            .WithMessage("Database test query is required");
            
        RuleFor(x => x.MaxRetryAttempts)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Max retry attempts cannot be negative")
            .LessThanOrEqualTo(5)
            .WithMessage("Max retry attempts cannot exceed 5");
            
        RuleFor(x => x.RetryDelay)
            .GreaterThanOrEqualTo(TimeSpan.Zero)
            .WithMessage("Retry delay cannot be negative")
            .LessThanOrEqualTo(TimeSpan.FromSeconds(10))
            .WithMessage("Retry delay cannot exceed 10 seconds");
    }
}

public sealed class RedisHealthOptionsValidator : AbstractValidator<RedisHealthOptions>
{
    public RedisHealthOptionsValidator()
    {
        RuleFor(x => x.ConnectionString)
            .NotEmpty()
            .When(x => x.Enabled)
            .WithMessage("Redis connection string is required when Redis health checks are enabled");
            
        RuleFor(x => x.Timeout)
            .GreaterThanOrEqualTo(TimeSpan.FromSeconds(1))
            .WithMessage("Redis timeout must be at least 1 second")
            .LessThanOrEqualTo(TimeSpan.FromSeconds(30))
            .WithMessage("Redis timeout cannot exceed 30 seconds");
            
        RuleFor(x => x.TestKey)
            .NotEmpty()
            .WithMessage("Redis test key is required");
            
        RuleFor(x => x.TestValue)
            .NotEmpty()
            .WithMessage("Redis test value is required");
            
        RuleFor(x => x.MaxRetryAttempts)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Max retry attempts cannot be negative")
            .LessThanOrEqualTo(5)
            .WithMessage("Max retry attempts cannot exceed 5");
            
        RuleFor(x => x.RetryDelay)
            .GreaterThanOrEqualTo(TimeSpan.Zero)
            .WithMessage("Retry delay cannot be negative")
            .LessThanOrEqualTo(TimeSpan.FromSeconds(10))
            .WithMessage("Retry delay cannot exceed 10 seconds");
    }
}

public sealed class MetricsOptionsValidator : AbstractValidator<MetricsOptions>
{
    public MetricsOptionsValidator()
    {
        RuleFor(x => x.MeterName)
            .NotEmpty()
            .WithMessage("Meter name is required");
            
        RuleFor(x => x.CollectionInterval)
            .GreaterThanOrEqualTo(TimeSpan.FromSeconds(5))
            .WithMessage("Collection interval must be at least 5 seconds")
            .LessThanOrEqualTo(TimeSpan.FromMinutes(5))
            .WithMessage("Collection interval cannot exceed 5 minutes")
            .When(x => x.Enabled);
    }
}