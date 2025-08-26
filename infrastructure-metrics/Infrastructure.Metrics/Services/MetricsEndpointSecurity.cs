using System.Collections.Concurrent;
using System.Net;

namespace Infrastructure.Metrics.Services;

public sealed class MetricsEndpointSecurity : IMetricsEndpointSecurity, IDisposable
{
    private readonly ILogger<MetricsEndpointSecurity> _logger;
    private readonly MetricsOptions _options;
    private readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimitCache = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> _blockedIps = new();
    private readonly Timer? _cleanupTimer;

    public MetricsEndpointSecurity(
        ILogger<MetricsEndpointSecurity> logger,
        IOptions<MetricsOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        // Start cleanup timer to remove expired rate limit entries and IP blocks
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, 
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public async Task<bool> IsAuthorizedAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        if (!_options.Security.EnableSecurity)
        {
            return true;
        }

        var validationResult = await ValidateRequestAsync(context.Request, cancellationToken);
        return validationResult.IsValid;
    }

    public async Task<SecurityValidationResult> ValidateRequestAsync(HttpRequest request, 
        CancellationToken cancellationToken = default)
    {
        var clientIp = GetClientIpAddress(request);
        var context = new Dictionary<string, string>
        {
            ["user_agent"] = request.Headers.UserAgent.ToString(),
            ["endpoint"] = request.Path,
            ["method"] = request.Method
        };

        // Check if IP is blocked
        if (_blockedIps.ContainsKey(clientIp))
        {
            _logger.LogDebug("IP {ClientIp} is currently blocked", clientIp);
            return new SecurityValidationResult
            {
                IsValid = false,
                Reason = "IP address is blocked",
                ValidationType = SecurityValidationType.IpAddress,
                ClientIp = clientIp,
                Context = context
            };
        }

        // Check IP allowlist if configured
        if (!await IsAllowedIpAsync(clientIp, cancellationToken))
        {
            await BlockIpTemporarily(clientIp, "IP not in allowlist");
            return new SecurityValidationResult
            {
                IsValid = false,
                Reason = "IP address not allowed",
                ValidationType = SecurityValidationType.IpAddress,
                ClientIp = clientIp,
                Context = context
            };
        }

        // Check authentication if required
        if (_options.Security.RequireAuthentication)
        {
            var hasValidAuth = await HasValidAuthenticationAsync(request, cancellationToken);
            if (!hasValidAuth)
            {
                return new SecurityValidationResult
                {
                    IsValid = false,
                    Reason = "Invalid authentication",
                    ValidationType = SecurityValidationType.Authentication,
                    ClientIp = clientIp,
                    Context = context
                };
            }
        }

        // Validate security headers
        var headerValidation = ValidateSecurityHeaders(request);
        if (!headerValidation.isValid)
        {
            return new SecurityValidationResult
            {
                IsValid = false,
                Reason = headerValidation.reason,
                ValidationType = SecurityValidationType.Headers,
                ClientIp = clientIp,
                Context = context
            };
        }

        return new SecurityValidationResult
        {
            IsValid = true,
            ValidationType = SecurityValidationType.Authorization,
            ClientIp = clientIp,
            Context = context
        };
    }

    public async Task<bool> IsAllowedIpAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        if (_options.Security.AllowedIps?.Length == 0)
        {
            // No IP restrictions configured
            return true;
        }

        if (_options.Security.AllowedIps == null)
        {
            return true;
        }

        foreach (var allowedPattern in _options.Security.AllowedIps)
        {
            if (IsIpMatch(ipAddress, allowedPattern))
            {
                _logger.LogDebug("IP {IpAddress} matches allowed pattern {Pattern}", ipAddress, allowedPattern);
                await Task.CompletedTask;
                return true;
            }
        }

        _logger.LogWarning("IP {IpAddress} not found in allowlist", ipAddress);
        await Task.CompletedTask;
        return false;
    }

    public async Task<bool> HasValidAuthenticationAsync(HttpRequest request, CancellationToken cancellationToken = default)
    {
        if (!_options.Security.RequireAuthentication)
        {
            return true;
        }

        var authHeader = request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader))
        {
            _logger.LogDebug("No authorization header present");
            return false;
        }

        // In a real implementation, this would validate the authentication token
        // For now, we just check that some form of authorization is present
        var isValid = !string.IsNullOrWhiteSpace(authHeader);
        
        if (!isValid)
        {
            _logger.LogWarning("Invalid authorization header format");
        }

        await Task.CompletedTask;
        return isValid;
    }

    public async Task LogSecurityEventAsync(SecurityEventType eventType, string clientIp, string? userAgent = null, 
        string? details = null, CancellationToken cancellationToken = default)
    {
        if (!_options.Security.LogSecurityEvents)
        {
            return;
        }

        var logLevel = eventType switch
        {
            SecurityEventType.UnauthorizedAccess => LogLevel.Warning,
            SecurityEventType.IpBlocked => LogLevel.Warning,
            SecurityEventType.RateLimitExceeded => LogLevel.Information,
            SecurityEventType.SuspiciousActivity => LogLevel.Warning,
            SecurityEventType.SecurityHeaderMissing => LogLevel.Information,
            _ => LogLevel.Information
        };

        _logger.Log(logLevel, "Security event {EventType} from {ClientIp}: {Details} (UserAgent: {UserAgent})",
            eventType, clientIp, details ?? "No details", userAgent ?? "Unknown");

        await Task.CompletedTask;
    }

    public string GenerateSecurityHeaders(HttpResponse response)
    {
        if (!_options.Security.EnableSecurityHeaders)
        {
            return string.Empty;
        }

        var headers = new Dictionary<string, string>
        {
            ["X-Content-Type-Options"] = "nosniff",
            ["X-Frame-Options"] = "DENY",
            ["X-XSS-Protection"] = "1; mode=block",
            ["Referrer-Policy"] = "no-referrer",
            ["Cache-Control"] = "no-cache, no-store, must-revalidate, private"
        };

        foreach (var kvp in headers)
        {
            if (!response.Headers.ContainsKey(kvp.Key))
            {
                response.Headers.Add(kvp.Key, kvp.Value);
            }
        }

        return string.Join("; ", headers.Select(h => $"{h.Key}={h.Value}"));
    }

    public bool ShouldRateLimitRequest(string clientIp, string endpoint)
    {
        if (!_options.Security.EnableRateLimiting)
        {
            return false;
        }

        var key = $"{clientIp}:{endpoint}";
        var now = DateTimeOffset.UtcNow;
        var windowStart = now.AddMinutes(-1); // 1-minute window

        var rateLimitInfo = _rateLimitCache.AddOrUpdate(key, 
            new RateLimitInfo { RequestCount = 1, WindowStart = now },
            (k, existing) =>
            {
                // Reset window if it's expired
                if (existing.WindowStart < windowStart)
                {
                    return new RateLimitInfo { RequestCount = 1, WindowStart = now };
                }

                return existing with { RequestCount = existing.RequestCount + 1 };
            });

        var isRateLimited = rateLimitInfo.RequestCount > _options.Security.MaxRequestsPerMinute;

        if (isRateLimited)
        {
            _logger.LogWarning("Rate limit exceeded for {ClientIp} on {Endpoint}: {Count}/{Limit} requests",
                clientIp, endpoint, rateLimitInfo.RequestCount, _options.Security.MaxRequestsPerMinute);
        }

        return isRateLimited;
    }

    public async Task<MetricsAccessAttempt> RecordAccessAttemptAsync(HttpRequest request, bool authorized,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var clientIp = GetClientIpAddress(request);
        
        var attempt = new MetricsAccessAttempt
        {
            ClientIp = clientIp,
            UserAgent = request.Headers.UserAgent.FirstOrDefault(),
            RequestPath = request.Path,
            HttpMethod = request.Method,
            Authorized = authorized,
            DenialReason = authorized ? null : "Security validation failed",
            ProcessingTime = stopwatch.Elapsed,
            Headers = request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value)),
            CorrelationId = Activity.Current?.Id
        };

        _logger.LogDebug("Recorded access attempt {AttemptId} for {ClientIp}: {Authorized}",
            attempt.Id, clientIp, authorized ? "Authorized" : "Denied");

        await Task.CompletedTask;
        return attempt;
    }

    private static string GetClientIpAddress(HttpRequest request)
    {
        var xForwardedFor = request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            var forwardedIps = xForwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (forwardedIps.Length > 0)
            {
                return forwardedIps[0].Trim();
            }
        }

        var xRealIp = request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xRealIp))
        {
            return xRealIp;
        }

        // This would normally use HttpContext, but we're in a request context
        return "unknown";
    }

    private static bool IsIpMatch(string ipAddress, string pattern)
    {
        // Handle exact IP match
        if (ipAddress == pattern)
        {
            return true;
        }

        // Handle CIDR notation
        if (pattern.Contains('/'))
        {
            return IsIpInCidrRange(ipAddress, pattern);
        }

        // Handle wildcard patterns (simple implementation)
        if (pattern.Contains('*'))
        {
            var regexPattern = pattern.Replace("*", ".*");
            return System.Text.RegularExpressions.Regex.IsMatch(ipAddress, regexPattern);
        }

        return false;
    }

    private static bool IsIpInCidrRange(string ipAddress, string cidrRange)
    {
        try
        {
            var parts = cidrRange.Split('/');
            if (parts.Length != 2) return false;

            var networkAddr = IPAddress.Parse(parts[0]);
            var prefixLength = int.Parse(parts[1]);
            var testAddr = IPAddress.Parse(ipAddress);

            var networkBytes = networkAddr.GetAddressBytes();
            var testBytes = testAddr.GetAddressBytes();

            if (networkBytes.Length != testBytes.Length) return false;

            var mask = CreateSubnetMask(prefixLength, networkBytes.Length);

            for (int i = 0; i < networkBytes.Length; i++)
            {
                if ((networkBytes[i] & mask[i]) != (testBytes[i] & mask[i]))
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static byte[] CreateSubnetMask(int prefixLength, int addressLength)
    {
        var mask = new byte[addressLength];
        int bytesToFill = prefixLength / 8;
        int bitsInLastByte = prefixLength % 8;

        for (int i = 0; i < bytesToFill; i++)
        {
            mask[i] = 0xFF;
        }

        if (bytesToFill < addressLength && bitsInLastByte > 0)
        {
            mask[bytesToFill] = (byte)(0xFF << (8 - bitsInLastByte));
        }

        return mask;
    }

    private (bool isValid, string? reason) ValidateSecurityHeaders(HttpRequest request)
    {
        // Check for suspicious user agents
        var userAgent = request.Headers.UserAgent.FirstOrDefault();
        if (string.IsNullOrEmpty(userAgent))
        {
            return (false, "Missing User-Agent header");
        }

        // Check for suspicious patterns in user agent
        var suspiciousPatterns = new[] { "bot", "crawler", "spider", "scraper" };
        if (suspiciousPatterns.Any(pattern => userAgent.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
        {
            return (false, "Suspicious User-Agent detected");
        }

        return (true, null);
    }

    private async Task BlockIpTemporarily(string ipAddress, string reason)
    {
        var blockUntil = DateTimeOffset.UtcNow.Add(_options.Security.IpBlockDuration);
        _blockedIps.TryAdd(ipAddress, blockUntil);

        await LogSecurityEventAsync(SecurityEventType.IpBlocked, ipAddress, null, 
            $"IP blocked for {_options.Security.IpBlockDuration}: {reason}");

        _logger.LogWarning("Temporarily blocked IP {IpAddress} until {BlockUntil}: {Reason}",
            ipAddress, blockUntil, reason);
    }

    private void CleanupExpiredEntries(object? state)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var expiredRateLimits = 0;
            var expiredBlocks = 0;

            // Cleanup expired rate limit entries
            var expiredRateLimitKeys = _rateLimitCache
                .Where(kvp => kvp.Value.WindowStart < now.AddMinutes(-2))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredRateLimitKeys)
            {
                if (_rateLimitCache.TryRemove(key, out _))
                {
                    expiredRateLimits++;
                }
            }

            // Cleanup expired IP blocks
            var expiredBlockKeys = _blockedIps
                .Where(kvp => kvp.Value < now)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var ip in expiredBlockKeys)
            {
                if (_blockedIps.TryRemove(ip, out _))
                {
                    expiredBlocks++;
                    _logger.LogInformation("Unblocked IP {IpAddress}", ip);
                }
            }

            if (expiredRateLimits > 0 || expiredBlocks > 0)
            {
                _logger.LogDebug("Cleaned up {RateLimits} expired rate limit entries and {Blocks} expired IP blocks",
                    expiredRateLimits, expiredBlocks);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during security cleanup");
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        _rateLimitCache.Clear();
        _blockedIps.Clear();
    }

    private sealed record RateLimitInfo
    {
        public int RequestCount { get; init; }
        public DateTimeOffset WindowStart { get; init; }
    }
}