namespace Infrastructure.Metrics.Configuration;

public sealed class MetricsOptionsValidator : AbstractValidator<MetricsOptions>
{
    public MetricsOptionsValidator()
    {
        RuleFor(x => x.MetricsPath)
            .NotEmpty()
            .WithMessage("Metrics path is required")
            .Must(BeValidPath)
            .WithMessage("Metrics path must start with '/'");
            
        RuleFor(x => x.ServiceName)
            .NotEmpty()
            .WithMessage("Service name is required")
            .Must(BeValidMetricName)
            .WithMessage("Service name must be valid for Prometheus labels");
            
        RuleFor(x => x.ServiceVersion)
            .NotEmpty()
            .WithMessage("Service version is required");
            
        RuleFor(x => x.Environment)
            .NotEmpty()
            .WithMessage("Environment is required")
            .Must(BeValidMetricValue)
            .WithMessage("Environment must be valid for Prometheus labels");
            
        RuleFor(x => x.ExportInterval)
            .GreaterThanOrEqualTo(TimeSpan.FromSeconds(1))
            .WithMessage("Export interval must be at least 1 second")
            .LessThanOrEqualTo(TimeSpan.FromMinutes(5))
            .WithMessage("Export interval cannot exceed 5 minutes");
            
        RuleFor(x => x.ExportTimeout)
            .GreaterThanOrEqualTo(TimeSpan.FromSeconds(5))
            .WithMessage("Export timeout must be at least 5 seconds")
            .LessThanOrEqualTo(TimeSpan.FromMinutes(2))
            .WithMessage("Export timeout cannot exceed 2 minutes");
            
        RuleFor(x => x.MaxConcurrentExports)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Max concurrent exports must be at least 1")
            .LessThanOrEqualTo(100)
            .WithMessage("Max concurrent exports cannot exceed 100");
        
        RuleFor(x => x.Security)
            .SetValidator(new SecurityOptionsValidator());
            
        RuleFor(x => x.Prometheus)
            .SetValidator(new PrometheusOptionsValidator());
            
        RuleFor(x => x.ServiceDiscovery)
            .SetValidator(new ServiceDiscoveryOptionsValidator());
            
        RuleFor(x => x.CustomMetrics)
            .SetValidator(new CustomMetricsOptionsValidator());
            
        RuleFor(x => x.Performance)
            .SetValidator(new PerformanceOptionsValidator());
    }
    
    private static bool BeValidPath(string path)
    {
        return !string.IsNullOrEmpty(path) && path.StartsWith('/');
    }
    
    private static bool BeValidMetricName(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        
        // Prometheus metric names must match [a-zA-Z_:][a-zA-Z0-9_:]*
        if (!char.IsLetter(name[0]) && name[0] != '_' && name[0] != ':')
            return false;
            
        return name.All(c => char.IsLetterOrDigit(c) || c == '_' || c == ':');
    }
    
    private static bool BeValidMetricValue(string value)
    {
        return !string.IsNullOrEmpty(value) && !value.Contains('\n') && !value.Contains('\t');
    }
}

public sealed class SecurityOptionsValidator : AbstractValidator<SecurityOptions>
{
    public SecurityOptionsValidator()
    {
        RuleFor(x => x.AllowedIps)
            .Must(BeValidIpAddresses)
            .When(x => x.AllowedIps != null && x.AllowedIps.Length > 0)
            .WithMessage("All allowed IPs must be valid IP addresses or CIDR ranges");
            
        RuleFor(x => x.MaxRequestsPerMinute)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Max requests per minute must be at least 1")
            .LessThanOrEqualTo(10000)
            .WithMessage("Max requests per minute cannot exceed 10,000")
            .When(x => x.EnableRateLimiting);
            
        RuleFor(x => x.IpBlockDuration)
            .GreaterThanOrEqualTo(TimeSpan.FromMinutes(1))
            .WithMessage("IP block duration must be at least 1 minute")
            .LessThanOrEqualTo(TimeSpan.FromDays(1))
            .WithMessage("IP block duration cannot exceed 1 day");
    }
    
    private static bool BeValidIpAddresses(string[]? ipAddresses)
    {
        if (ipAddresses == null) return true;
        
        return ipAddresses.All(ip => 
            System.Net.IPAddress.TryParse(ip, out _) || 
            IsValidCidr(ip));
    }
    
    private static bool IsValidCidr(string cidr)
    {
        var parts = cidr.Split('/');
        if (parts.Length != 2) return false;
        
        return System.Net.IPAddress.TryParse(parts[0], out _) && 
               int.TryParse(parts[1], out var prefix) && 
               prefix >= 0 && prefix <= 32;
    }
}

public sealed class PrometheusOptionsValidator : AbstractValidator<PrometheusOptions>
{
    public PrometheusOptionsValidator()
    {
        RuleFor(x => x.ExporterType)
            .NotEmpty()
            .WithMessage("Exporter type is required")
            .Must(BeValidExporterType)
            .WithMessage("Exporter type must be 'AspNetCore' or 'HttpListener'");
            
        RuleFor(x => x.ExternalUrl)
            .Must(BeValidUrl)
            .When(x => !string.IsNullOrEmpty(x.ExternalUrl))
            .WithMessage("External URL must be a valid HTTP/HTTPS URL");
            
        RuleFor(x => x.ScrapeConfigRefreshInterval)
            .GreaterThanOrEqualTo(TimeSpan.FromSeconds(30))
            .WithMessage("Scrape config refresh interval must be at least 30 seconds")
            .LessThanOrEqualTo(TimeSpan.FromHours(1))
            .WithMessage("Scrape config refresh interval cannot exceed 1 hour");
    }
    
    private static bool BeValidExporterType(string exporterType)
    {
        return exporterType is "AspNetCore" or "HttpListener";
    }
    
    private static bool BeValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var result) && 
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}

public sealed class ServiceDiscoveryOptionsValidator : AbstractValidator<ServiceDiscoveryOptions>
{
    public ServiceDiscoveryOptionsValidator()
    {
        RuleFor(x => x.DiscoveryMethod)
            .NotEmpty()
            .WithMessage("Discovery method is required")
            .Must(BeValidDiscoveryMethod)
            .WithMessage("Discovery method must be 'Aspire', 'Consul', 'Static', or 'Kubernetes'");
            
        RuleFor(x => x.DiscoveryInterval)
            .GreaterThanOrEqualTo(TimeSpan.FromSeconds(10))
            .WithMessage("Discovery interval must be at least 10 seconds")
            .LessThanOrEqualTo(TimeSpan.FromMinutes(10))
            .WithMessage("Discovery interval cannot exceed 10 minutes");
            
        RuleFor(x => x.PrometheusConfigPath)
            .NotEmpty()
            .WithMessage("Prometheus config path is required")
            .When(x => x.GeneratePrometheusConfig);
    }
    
    private static bool BeValidDiscoveryMethod(string method)
    {
        return method is "Aspire" or "Consul" or "Static" or "Kubernetes";
    }
}

public sealed class CustomMetricsOptionsValidator : AbstractValidator<CustomMetricsOptions>
{
    public CustomMetricsOptionsValidator()
    {
        RuleFor(x => x.MeterName)
            .NotEmpty()
            .WithMessage("Meter name is required");
            
        RuleFor(x => x.MeterVersion)
            .NotEmpty()
            .WithMessage("Meter version is required");
            
        RuleFor(x => x.MaxCustomMetrics)
            .GreaterThanOrEqualTo(10)
            .WithMessage("Max custom metrics must be at least 10")
            .LessThanOrEqualTo(10000)
            .WithMessage("Max custom metrics cannot exceed 10,000");
            
        RuleFor(x => x.MetricRetention)
            .GreaterThanOrEqualTo(TimeSpan.FromMinutes(5))
            .WithMessage("Metric retention must be at least 5 minutes")
            .LessThanOrEqualTo(TimeSpan.FromDays(1))
            .WithMessage("Metric retention cannot exceed 1 day");
    }
}

public sealed class PerformanceOptionsValidator : AbstractValidator<PerformanceOptions>
{
    public PerformanceOptionsValidator()
    {
        RuleFor(x => x.MetricsBufferSize)
            .GreaterThanOrEqualTo(1000)
            .WithMessage("Metrics buffer size must be at least 1,000")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Metrics buffer size cannot exceed 1,000,000");
            
        RuleFor(x => x.MetricsFlushInterval)
            .GreaterThanOrEqualTo(TimeSpan.FromSeconds(1))
            .WithMessage("Metrics flush interval must be at least 1 second")
            .LessThanOrEqualTo(TimeSpan.FromMinutes(1))
            .WithMessage("Metrics flush interval cannot exceed 1 minute");
            
        RuleFor(x => x.MaxResponseSize)
            .GreaterThanOrEqualTo(1024 * 1024) // 1MB
            .WithMessage("Max response size must be at least 1MB")
            .LessThanOrEqualTo(200 * 1024 * 1024) // 200MB
            .WithMessage("Max response size cannot exceed 200MB");
            
        RuleFor(x => x.CacheDuration)
            .GreaterThanOrEqualTo(TimeSpan.FromSeconds(1))
            .WithMessage("Cache duration must be at least 1 second")
            .LessThanOrEqualTo(TimeSpan.FromMinutes(5))
            .WithMessage("Cache duration cannot exceed 5 minutes")
            .When(x => x.EnableCaching);
    }
}