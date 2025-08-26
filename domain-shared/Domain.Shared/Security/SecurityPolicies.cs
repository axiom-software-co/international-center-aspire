namespace Shared.Security;

public static class SecurityPolicies
{
    // Zero-trust base policies
    public const string DefaultFallback = "DefaultFallback";
    public const string AuthenticatedUser = "AuthenticatedUser";
    public const string RequireMfa = "RequireMfa";
    public const string RequireSessionValidation = "RequireSessionValidation";
    
    // Admin policies (medical-grade compliance)
    public const string AdminAccess = "AdminAccess";
    public const string AdminCreate = "AdminCreate";
    public const string AdminUpdate = "AdminUpdate";
    public const string AdminDelete = "AdminDelete";
    public const string AdminExport = "AdminExport";
    public const string AdminSystemAccess = "AdminSystemAccess";
    
    // API-specific policies
    public const string ServicesRead = "ServicesRead";
    public const string ServicesWrite = "ServicesWrite";
    public const string NewsRead = "NewsRead";
    public const string NewsWrite = "NewsWrite";
    public const string EventsRead = "EventsRead";
    public const string EventsWrite = "EventsWrite";
    public const string ResearchRead = "ResearchRead";
    public const string ResearchWrite = "ResearchWrite";
    public const string ContactsRead = "ContactsRead";
    public const string ContactsWrite = "ContactsWrite";
    public const string SearchAccess = "SearchAccess";
    public const string NewsletterAccess = "NewsletterAccess";
    
    // High-security operations
    public const string AuditAccess = "AuditAccess";
    public const string SystemConfiguration = "SystemConfiguration";
    public const string UserManagement = "UserManagement";
}

public static class SecurityClaims
{
    // Standard claims
    public const string UserId = "user_id";
    public const string Role = "role";
    public const string Permissions = "permissions";
    public const string SessionId = "session_id";
    
    // Medical-grade compliance claims
    public const string MfaVerified = "mfa_verified";
    public const string IpAddress = "ip_address";
    public const string DeviceId = "device_id";
    public const string LastActivity = "last_activity";
    public const string SecurityLevel = "security_level";
    public const string AdminLevel = "admin_level";
    
    // API access claims
    public const string ApiAccess = "api_access";
    public const string ServiceAccess = "service_access";
    public const string DataAccess = "data_access";
}

public static class SecurityRoles
{
    // Public roles
    public const string Anonymous = "Anonymous";
    public const string User = "User";
    public const string Member = "Member";
    
    // Admin roles (hierarchical)
    public const string Admin = "Admin";
    public const string SuperAdmin = "SuperAdmin";
    public const string SystemAdmin = "SystemAdmin";
    
    // Service-specific roles
    public const string ServiceManager = "ServiceManager";
    public const string NewsManager = "NewsManager";
    public const string EventManager = "EventManager";
    public const string ResearchManager = "ResearchManager";
    public const string ContactManager = "ContactManager";
}

public static class SecurityLevels
{
    public const string Public = "Public";
    public const string Internal = "Internal";
    public const string Confidential = "Confidential";
    public const string Restricted = "Restricted";
    public const string TopSecret = "TopSecret";
}

public class SecurityContext
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = SecurityRoles.Anonymous;
    public List<string> Permissions { get; set; } = new();
    public string SessionId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    public string SecurityLevel { get; set; } = SecurityLevels.Public;
    public bool MfaVerified { get; set; } = false;
    public bool SessionValid { get; set; } = false;
    public string AdminLevel { get; set; } = string.Empty;
    public List<string> ApiAccess { get; set; } = new();
}