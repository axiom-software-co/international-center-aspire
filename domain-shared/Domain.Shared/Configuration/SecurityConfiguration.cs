namespace Shared.Configuration;

public class SecurityConfiguration
{
    public JwtConfiguration Jwt { get; set; } = new();
    public bool RequireHttps { get; set; } = true;
    public bool AllowHttp { get; set; } = false;
    public long MaxRequestSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB
    public string[] BlockedIpAddresses { get; set; } = Array.Empty<string>();
    public string[] AdminAllowedIpAddresses { get; set; } = Array.Empty<string>();
    public RateLimitConfiguration RateLimit { get; set; } = new();
    public SessionConfiguration Session { get; set; } = new();
    
    // Additional security properties for health checks
    public bool EnableZeroTrust { get; set; } = true;
    public bool EnableSecurityHeaders { get; set; } = true;
    public bool EnableRateLimiting { get; set; } = true;
    
    // Legacy property accessor for health checks
    public string JwtSecretKey 
    { 
        get => Jwt.SecretKey; 
        set => Jwt.SecretKey = value; 
    }
}

public class JwtConfiguration
{
    public string Issuer { get; set; } = "InternationalCenter";
    public string Audience { get; set; } = "InternationalCenter";
    public string SecretKey { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 7;
    public bool RequireHttpsMetadata { get; set; } = true;
}

public class RateLimitConfiguration
{
    public bool Enabled { get; set; } = true;
    public int RequestsPerMinute { get; set; } = 100;
    public int RequestsPerHour { get; set; } = 1000;
    public int BurstSize { get; set; } = 10;
    public string[] ExemptIpAddresses { get; set; } = Array.Empty<string>();
}

public class SessionConfiguration
{
    public TimeSpan MaxAge { get; set; } = TimeSpan.FromHours(8);
    public TimeSpan MaxInactivity { get; set; } = TimeSpan.FromMinutes(30);
    public bool RequireMfaForAdmin { get; set; } = true;
    public bool RequireSessionValidation { get; set; } = true;
}