using System.Diagnostics;

namespace Shared.Infrastructure.Observability;

public static class ActivitySources
{
    // Create activity sources for each domain
    public static readonly ActivitySource ServicesApi = new("InternationalCenter.Services.Public.Api", "1.0.0");
    public static readonly ActivitySource ServicesAdminApi = new("InternationalCenter.Services.Admin.Api", "1.0.0");
    public static readonly ActivitySource NewsApi = new("InternationalCenter.News.Api", "1.0.0");
    public static readonly ActivitySource ContactsApi = new("InternationalCenter.Contacts.Api", "1.0.0");
    public static readonly ActivitySource ResearchApi = new("InternationalCenter.Research.Api", "1.0.0");
    public static readonly ActivitySource SearchApi = new("InternationalCenter.Search.Api", "1.0.0");
    public static readonly ActivitySource EventsApi = new("InternationalCenter.Events.Api", "1.0.0");
    public static readonly ActivitySource NewsletterApi = new("InternationalCenter.Newsletter.Api", "1.0.0");
    
    // Shared infrastructure sources
    public static readonly ActivitySource Database = new("InternationalCenter.Database", "1.0.0");
    public static readonly ActivitySource Cache = new("InternationalCenter.Cache", "1.0.0");
    
    // Get activity source by service name
    public static ActivitySource GetByServiceName(string serviceName) => serviceName switch
    {
        "Services" or "Services.Api" => ServicesApi,
        "Services.Admin.Api" => ServicesAdminApi,
        "News" or "News.Api" => NewsApi,
        "Contacts" or "Contacts.Api" => ContactsApi,
        "Research" or "Research.Api" => ResearchApi,
        "Search" or "Search.Api" => SearchApi,
        "Events" or "Events.Api" => EventsApi,
        "Newsletter" or "Newsletter.Api" => NewsletterApi,
        "Database" => Database,
        "Cache" => Cache,
        _ => throw new ArgumentException($"Unknown service name: {serviceName}")
    };
    
    public static void DisposeAll()
    {
        ServicesApi.Dispose();
        ServicesAdminApi.Dispose();
        NewsApi.Dispose();
        ContactsApi.Dispose();
        ResearchApi.Dispose();
        SearchApi.Dispose();
        EventsApi.Dispose();
        NewsletterApi.Dispose();
        Database.Dispose();
        Cache.Dispose();
    }
}