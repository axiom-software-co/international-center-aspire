using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InternationalCenter.Shared.Configuration;

/// <summary>
/// Aspire-native service discovery configuration for frontend clients
/// Provides type-safe access to service endpoints with enhanced discoverability
/// </summary>
public sealed class ServiceDiscoveryConfiguration
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ServiceDiscoveryConfiguration> _logger;
    
    // Service endpoint constants - Aspire will populate these via environment variables
    public const string SERVICES_API_KEY = "services:services-api:publicapi:0";
    public const string SERVICES_ADMIN_API_KEY = "services:services-admin-api:adminapi:0";
    public const string NEWS_API_KEY = "services:news-api:newsapi:0";
    public const string CONTACTS_API_KEY = "services:contacts-api:contactsapi:0";
    public const string RESEARCH_API_KEY = "services:research-api:researchapi:0";
    public const string SEARCH_API_KEY = "services:search-api:searchapi:0";
    public const string EVENTS_API_KEY = "services:events-api:eventsapi:0";
    public const string NEWSLETTER_API_KEY = "services:newsletter-api:newsletterapi:0";

    public ServiceDiscoveryConfiguration(IConfiguration configuration, ILogger<ServiceDiscoveryConfiguration> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Gets Services API endpoint with enhanced Aspire discovery
    /// </summary>
    public string ServicesApiUrl => GetServiceUrl(SERVICES_API_KEY, "SERVICES_API") 
        ?? throw new InvalidOperationException("Services API endpoint not configured");

    /// <summary>
    /// Gets Services Admin API endpoint (secure admin operations)
    /// </summary>
    public string ServicesAdminApiUrl => GetServiceUrl(SERVICES_ADMIN_API_KEY, "SERVICES_ADMIN_API") 
        ?? throw new InvalidOperationException("Services Admin API endpoint not configured");

    /// <summary>
    /// Gets News API endpoint
    /// </summary>
    public string NewsApiUrl => GetServiceUrl(NEWS_API_KEY, "NEWS_API") 
        ?? throw new InvalidOperationException("News API endpoint not configured");

    /// <summary>
    /// Gets Contacts API endpoint
    /// </summary>
    public string ContactsApiUrl => GetServiceUrl(CONTACTS_API_KEY, "CONTACTS_API") 
        ?? throw new InvalidOperationException("Contacts API endpoint not configured");

    /// <summary>
    /// Gets Research API endpoint
    /// </summary>
    public string ResearchApiUrl => GetServiceUrl(RESEARCH_API_KEY, "RESEARCH_API") 
        ?? throw new InvalidOperationException("Research API endpoint not configured");

    /// <summary>
    /// Gets Search API endpoint
    /// </summary>
    public string SearchApiUrl => GetServiceUrl(SEARCH_API_KEY, "SEARCH_API") 
        ?? throw new InvalidOperationException("Search API endpoint not configured");

    /// <summary>
    /// Gets Events API endpoint
    /// </summary>
    public string EventsApiUrl => GetServiceUrl(EVENTS_API_KEY, "EVENTS_API") 
        ?? throw new InvalidOperationException("Events API endpoint not configured");

    /// <summary>
    /// Gets Newsletter API endpoint
    /// </summary>
    public string NewsletterApiUrl => GetServiceUrl(NEWSLETTER_API_KEY, "NEWSLETTER_API") 
        ?? throw new InvalidOperationException("Newsletter API endpoint not configured");

    /// <summary>
    /// Gets all configured service endpoints for debugging and health monitoring
    /// </summary>
    public Dictionary<string, string> GetAllServiceEndpoints()
    {
        return new Dictionary<string, string>
        {
            [nameof(ServicesApiUrl)] = ServicesApiUrl,
            [nameof(ServicesAdminApiUrl)] = ServicesAdminApiUrl,
            [nameof(NewsApiUrl)] = NewsApiUrl,
            [nameof(ContactsApiUrl)] = ContactsApiUrl,
            [nameof(ResearchApiUrl)] = ResearchApiUrl,
            [nameof(SearchApiUrl)] = SearchApiUrl,
            [nameof(EventsApiUrl)] = EventsApiUrl,
            [nameof(NewsletterApiUrl)] = NewsletterApiUrl
        };
    }

    /// <summary>
    /// Validates that all required services are discoverable
    /// </summary>
    public bool ValidateServiceDiscovery()
    {
        var endpoints = GetAllServiceEndpoints();
        var validCount = 0;

        foreach (var (serviceName, endpoint) in endpoints)
        {
            if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            {
                _logger.LogInformation("✅ Service discovered: {ServiceName} -> {Endpoint}", serviceName, endpoint);
                validCount++;
            }
            else
            {
                _logger.LogError("❌ Service discovery failed: {ServiceName} -> {Endpoint}", serviceName, endpoint);
            }
        }

        var isValid = validCount == endpoints.Count;
        _logger.LogInformation("🔍 Service Discovery Validation: {Valid}/{Total} services discovered", 
            validCount, endpoints.Count);

        return isValid;
    }

    private string? GetServiceUrl(string aspireKey, string fallbackEnvKey)
    {
        // Try Aspire-native service discovery first (preferred)
        var aspireUrl = _configuration[aspireKey];
        if (!string.IsNullOrWhiteSpace(aspireUrl))
        {
            _logger.LogDebug("🎯 Using Aspire service discovery: {Key} -> {Url}", aspireKey, aspireUrl);
            return aspireUrl;
        }

        // Fallback to environment variable (for compatibility)
        var envUrl = _configuration[fallbackEnvKey];
        if (!string.IsNullOrWhiteSpace(envUrl))
        {
            _logger.LogDebug("🔄 Using environment variable fallback: {Key} -> {Url}", fallbackEnvKey, envUrl);
            return envUrl;
        }

        _logger.LogWarning("⚠️ Service endpoint not found: {AspireKey} or {EnvKey}", aspireKey, fallbackEnvKey);
        return null;
    }
}