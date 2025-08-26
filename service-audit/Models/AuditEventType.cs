namespace Service.Audit.Models;

public enum AuditEventType
{
    Created = 1,
    Updated = 2,
    Deleted = 3,
    Read = 4,
    Login = 5,
    Logout = 6,
    PasswordChange = 7,
    PermissionChange = 8,
    SystemEvent = 9,
    SecurityEvent = 10,
    ConfigurationChange = 11,
    DataExport = 12,
    DataImport = 13,
    Backup = 14,
    Restore = 15
}