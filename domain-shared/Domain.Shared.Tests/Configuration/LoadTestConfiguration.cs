namespace InternationalCenter.Tests.Shared.Configuration;

/// <summary>
/// Centralized configuration for load testing across Services APIs
/// Provides consistent performance thresholds and test parameters
/// Medical-grade performance requirements for public website and admin portal
/// </summary>
public static class LoadTestConfiguration
{
    /// <summary>
    /// Performance thresholds for Public API (public website usage)
    /// Optimized for website visitor experience and search engine requirements
    /// </summary>
    public static class PublicApi
    {
        /// <summary>
        /// Maximum acceptable mean response time for public API operations (500ms)
        /// Based on Google Core Web Vitals and website performance standards
        /// </summary>
        public static readonly TimeSpan MaxMeanResponseTime = TimeSpan.FromMilliseconds(500);
        
        /// <summary>
        /// Maximum acceptable 95th percentile response time for public API (1000ms)
        /// Ensures 95% of website visitors get sub-second response times
        /// </summary>
        public static readonly TimeSpan Max95thPercentileResponseTime = TimeSpan.FromMilliseconds(1000);
        
        /// <summary>
        /// Maximum acceptable error rate for public API operations (1%)
        /// Public website requires high reliability for visitor experience
        /// </summary>
        public static readonly double MaxErrorRate = 0.01; // 1%
        
        /// <summary>
        /// Maximum acceptable mean response time under stress conditions (2000ms)
        /// Allows for some degradation under traffic spikes while maintaining usability
        /// </summary>
        public static readonly TimeSpan MaxStressResponseTime = TimeSpan.FromMilliseconds(2000);
        
        /// <summary>
        /// Maximum acceptable error rate under stress conditions (5%)
        /// Higher tolerance under stress but still maintains service availability
        /// </summary>
        public static readonly double MaxStressErrorRate = 0.05; // 5%
        
        /// <summary>
        /// Concurrent user load for normal operations testing
        /// Represents expected peak concurrent website visitors
        /// </summary>
        public static readonly int NormalConcurrentUsers = 100;
        
        /// <summary>
        /// Concurrent user load for stress testing  
        /// Represents traffic spike scenarios (social media viral content, etc.)
        /// </summary>
        public static readonly int StressConcurrentUsers = 250;
        
        /// <summary>
        /// Duration for sustained load testing
        /// Validates performance consistency over typical usage periods
        /// </summary>
        public static readonly TimeSpan SustainedLoadDuration = TimeSpan.FromMinutes(2);
        
        /// <summary>
        /// Duration for endurance testing
        /// Validates long-term stability and memory management
        /// </summary>
        public static readonly TimeSpan EnduranceDuration = TimeSpan.FromMinutes(4);
    }
    
    /// <summary>
    /// Performance thresholds for Admin API (medical-grade administrative operations)
    /// Stricter requirements due to medical compliance and data integrity needs
    /// </summary>
    public static class AdminApi
    {
        /// <summary>
        /// Maximum acceptable mean response time for admin operations (300ms)
        /// Medical-grade responsiveness for administrative efficiency
        /// </summary>
        public static readonly TimeSpan MaxMeanResponseTime = TimeSpan.FromMilliseconds(300);
        
        /// <summary>
        /// Maximum acceptable 95th percentile response time for admin operations (500ms)
        /// Ensures consistent admin user experience for medical professionals
        /// </summary>
        public static readonly TimeSpan Max95thPercentileResponseTime = TimeSpan.FromMilliseconds(500);
        
        /// <summary>
        /// Maximum acceptable error rate for admin operations (0%)
        /// Medical-grade systems require zero tolerance for operation failures
        /// </summary>
        public static readonly double MaxErrorRate = 0.0; // 0% - zero tolerance
        
        /// <summary>
        /// Maximum acceptable mean response time for audit-heavy operations (600ms)
        /// Allows for additional audit trail processing while maintaining responsiveness
        /// </summary>
        public static readonly TimeSpan MaxAuditResponseTime = TimeSpan.FromMilliseconds(600);
        
        /// <summary>
        /// Maximum acceptable error rate for audit operations (1%)
        /// Slightly higher tolerance for audit-intensive operations while maintaining compliance
        /// </summary>
        public static readonly double MaxAuditErrorRate = 0.01; // 1%
        
        /// <summary>
        /// Maximum acceptable error rate for concurrent modification scenarios (10%)
        /// Allows for conflict resolution in concurrent editing scenarios
        /// </summary>
        public static readonly double MaxConcurrencyErrorRate = 0.10; // 10%
        
        /// <summary>
        /// Concurrent administrator load for normal operations
        /// Represents expected peak concurrent admin users in medical facilities
        /// </summary>
        public static readonly int NormalConcurrentAdmins = 50;
        
        /// <summary>
        /// Concurrent operations for audit testing
        /// Moderate load focused on audit trail generation performance
        /// </summary>
        public static readonly int AuditConcurrentOperations = 20;
        
        /// <summary>
        /// Concurrent operations for modification testing
        /// Tests concurrent editing and conflict resolution
        /// </summary>
        public static readonly int ConcurrencyTestOperations = 30;
        
        /// <summary>
        /// Duration for sustained admin load testing
        /// Validates performance during extended administrative sessions
        /// </summary>
        public static readonly TimeSpan SustainedLoadDuration = TimeSpan.FromMinutes(2);
        
        /// <summary>
        /// Duration for audit load testing
        /// Validates audit trail performance over typical usage periods
        /// </summary>
        public static readonly TimeSpan AuditLoadDuration = TimeSpan.FromMinutes(2);
    }
    
    /// <summary>
    /// Common load test simulation patterns
    /// Reusable load patterns for consistent testing approach
    /// </summary>
    public static class SimulationPatterns
    {
        /// <summary>
        /// Standard ramp-up rate (requests per second)
        /// Gradual increase to avoid overwhelming the system during startup
        /// </summary>
        public static readonly int RampUpRate = 10;
        
        /// <summary>
        /// Standard ramp-down rate (requests per second)
        /// Gradual decrease for clean test completion
        /// </summary>
        public static readonly int RampDownRate = 5;
        
        /// <summary>
        /// Ramp-up duration for load tests
        /// Time to gradually increase load to target levels
        /// </summary>
        public static readonly TimeSpan RampUpDuration = TimeSpan.FromSeconds(30);
        
        /// <summary>
        /// Ramp-down duration for load tests
        /// Time to gradually decrease load for clean completion
        /// </summary>
        public static readonly TimeSpan RampDownDuration = TimeSpan.FromSeconds(30);
        
        /// <summary>
        /// Traffic spike rate (requests per second)
        /// Simulates sudden traffic increases (news events, social media)
        /// </summary>
        public static readonly int TrafficSpikeRate = 50;
        
        /// <summary>
        /// Peak traffic spike rate (requests per second)
        /// Maximum traffic spike for stress testing
        /// </summary>
        public static readonly int PeakSpikeRate = 100;
        
        /// <summary>
        /// Traffic spike duration
        /// How long traffic spikes typically last
        /// </summary>
        public static readonly TimeSpan SpikeDuration = TimeSpan.FromSeconds(30);
        
        /// <summary>
        /// Peak spike duration
        /// Duration of maximum traffic spikes
        /// </summary>
        public static readonly TimeSpan PeakSpikeDuration = TimeSpan.FromSeconds(20);
    }
    
    /// <summary>
    /// Test data configuration for load testing
    /// Standardizes test data volumes for consistent and realistic testing
    /// </summary>
    public static class TestData
    {
        /// <summary>
        /// Number of services to create for public API load testing
        /// Provides realistic dataset size for website browsing scenarios
        /// </summary>
        public static readonly int PublicApiServiceCount = 50;
        
        /// <summary>
        /// Number of services to create for admin API load testing  
        /// Smaller dataset appropriate for administrative operations
        /// </summary>
        public static readonly int AdminApiServiceCount = 25;
        
        /// <summary>
        /// Number of services for stress testing
        /// Larger dataset to test performance under data volume stress
        /// </summary>
        public static readonly int StressTestServiceCount = 100;
        
        /// <summary>
        /// Number of services for concurrency testing
        /// Small set for focused concurrent modification testing
        /// </summary>
        public static readonly int ConcurrencyTestServiceCount = 5;
        
        /// <summary>
        /// Number of services for endurance testing
        /// Moderate dataset for sustained operations over time
        /// </summary>
        public static readonly int EnduranceTestServiceCount = 25;
    }
    
    /// <summary>
    /// Report configuration for load test outputs
    /// Standardizes load test reporting across all test projects
    /// </summary>
    public static class Reporting
    {
        /// <summary>
        /// Base folder for load test reports
        /// Centralized location for all load test outputs
        /// </summary>
        public static readonly string ReportBaseFolder = "load-test-reports";
        
        /// <summary>
        /// Report formats to generate
        /// Provides both human-readable and machine-readable formats
        /// </summary>
        public static readonly NBomber.Contracts.ReportFormat[] ReportFormats = 
        {
            NBomber.Contracts.ReportFormat.Html,
            NBomber.Contracts.ReportFormat.Csv,
            NBomber.Contracts.ReportFormat.Json
        };
        
        /// <summary>
        /// Whether to include detailed timing information in reports
        /// Useful for performance analysis and optimization
        /// </summary>
        public static readonly bool IncludeTimingDetails = true;
        
        /// <summary>
        /// Whether to include system resource usage in reports
        /// Helps identify resource bottlenecks during load testing
        /// </summary>
        public static readonly bool IncludeResourceUsage = true;
    }
}