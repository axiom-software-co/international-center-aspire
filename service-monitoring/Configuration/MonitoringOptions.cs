namespace Service.Monitoring.Configuration;

public sealed class MonitoringOptions
{
    public const string SectionName = "Monitoring";
    
    public bool Enabled { get; set; } = true;
    public string HealthCheckPath { get; set; } = "/health";
    public string ReadinessPath { get; set; } = "/health/ready";
    public string LivenessPath { get; set; } = "/health/live";
    public TimeSpan HealthCheckTimeout { get; set; } = TimeSpan.FromSeconds(15);
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromMinutes(1);
    public bool DetailedErrors { get; set; } = false;
    public bool CacheResults { get; set; } = true;
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromSeconds(30);
    
    // Database health check settings
    public DatabaseHealthOptions Database { get; set; } = new();
    
    // Redis health check settings  
    public RedisHealthOptions Redis { get; set; } = new();
    
    // Metrics settings
    public MetricsOptions Metrics { get; set; } = new();
}

public sealed class DatabaseHealthOptions
{
    public bool Enabled { get; set; } = true;
    public string? ConnectionString { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
    public string TestQuery { get; set; } = "SELECT 1";
    public bool CheckMigrations { get; set; } = true;
    public int MaxRetryAttempts { get; set; } = 2;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
}

public sealed class RedisHealthOptions
{
    public bool Enabled { get; set; } = true;
    public string? ConnectionString { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
    public string TestKey { get; set; } = "health-check";
    public string TestValue { get; set; } = "ok";
    public int MaxRetryAttempts { get; set; } = 2;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
}

public sealed class MetricsOptions
{
    public bool Enabled { get; set; } = true;
    public string MeterName { get; set; } = "Service.Monitoring";
    public bool CollectSystemMetrics { get; set; } = true;
    public bool CollectDatabaseMetrics { get; set; } = true;
    public bool CollectRedisMetrics { get; set; } = true;
    public TimeSpan CollectionInterval { get; set; } = TimeSpan.FromSeconds(15);
}