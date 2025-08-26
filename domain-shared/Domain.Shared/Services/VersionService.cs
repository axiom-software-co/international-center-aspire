using System.Reflection;

namespace Shared.Services;

public interface IVersionService
{
    string GetVersion();
    string GetFullVersion();
    DateTime BuildDate { get; }
    string BuildNumber { get; }
    string ShortGitSha { get; }
}

public class VersionService : IVersionService
{
    private readonly string _version;
    private readonly DateTime _buildDate;
    private readonly string _buildNumber;
    private readonly string _shortGitSha;

    public VersionService()
    {
        // Try to read from version file first (CI will generate this)
        var versionFilePath = Path.Combine(AppContext.BaseDirectory, "version.txt");
        
        if (File.Exists(versionFilePath))
        {
            var versionContent = File.ReadAllText(versionFilePath).Trim();
            var (buildDate, buildNumber, shortGitSha) = ParseVersionFromFile(versionContent);
            _buildDate = buildDate;
            _buildNumber = buildNumber;
            _shortGitSha = shortGitSha;
        }
        else
        {
            // Fallback for development - use assembly version and current date
            _buildDate = DateTime.UtcNow.Date;
            _buildNumber = "dev";
            _shortGitSha = "unknown";
        }

        _version = $"{_buildDate:yyyy.MM.dd}.{_buildNumber}.{_shortGitSha}";
    }

    private (DateTime buildDate, string buildNumber, string shortGitSha) ParseVersionFromFile(string versionContent)
    {
        try
        {
            // Expected format: 2025.01.24.123.abc1234
            var parts = versionContent.Split('.');
            
            if (parts.Length >= 5)
            {
                var year = int.Parse(parts[0]);
                var month = int.Parse(parts[1]);
                var day = int.Parse(parts[2]);
                var buildDate = new DateTime(year, month, day);
                var buildNumber = parts[3];
                var shortGitSha = parts[4];
                return (buildDate, buildNumber, shortGitSha);
            }
            else
            {
                // Fallback if file format is incorrect
                return (DateTime.UtcNow.Date, "dev", "unknown");
            }
        }
        catch
        {
            // Fallback if parsing fails
            return (DateTime.UtcNow.Date, "dev", "unknown");
        }
    }

    public string GetVersion() => _version;

    public string GetFullVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyVersion = assembly.GetName().Version?.ToString() ?? "1.0.0.0";
        
        return new
        {
            Version = _version,
            BuildDate = _buildDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            BuildNumber = _buildNumber,
            ShortGitSha = _shortGitSha,
            AssemblyVersion = assemblyVersion,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
        }.ToString() ?? string.Empty;
    }

    public DateTime BuildDate => _buildDate;
    public string BuildNumber => _buildNumber;
    public string ShortGitSha => _shortGitSha;
}