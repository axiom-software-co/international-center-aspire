using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using InternationalCenter.Services.Public.Api.Benchmarks.Benchmarks;

namespace InternationalCenter.Services.Public.Api.Benchmarks;

/// <summary>
/// BenchmarkDotNet runner for Services.Public.Api performance validation
/// WHY: Performance regressions may go undetected, compromises public website performance
/// SCOPE: Public API with Dapper repository patterns - critical for website user experience
/// CONTEXT: Public gateway architecture requires performance validation for website responsiveness
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("🚀 Services.Public.Api Performance Benchmarks");
        Console.WriteLine("=========================================");
        Console.WriteLine("WHY: Public website performance critical for user experience");
        Console.WriteLine("SCOPE: Dapper repositories, use cases, and API endpoints");
        Console.WriteLine("CONTEXT: Public gateway architecture with Aspire orchestration");
        Console.WriteLine();

        // Configure BenchmarkDotNet for production-realistic testing
        var config = DefaultConfig.Instance
            .AddJob(Job.Default
                .WithRuntime(CoreRuntime.Net90)
                .WithPlatform(Platform.X64)
                .WithToolchain(InProcessEmitToolchain.Instance)) // Faster execution for CI/CD
            .WithOptions(ConfigOptions.DisableLogFile); // Reduce output for cleaner CI logs

        // Run all benchmark classes
        var benchmarkSummaries = new[]
        {
            BenchmarkRunner.Run<RepositoryBenchmarks>(config),
            BenchmarkRunner.Run<UseCaseBenchmarks>(config),
            BenchmarkRunner.Run<ApiEndpointBenchmarks>(config)
        };

        Console.WriteLine();
        Console.WriteLine("📊 Benchmark Summary:");
        foreach (var summary in benchmarkSummaries)
        {
            Console.WriteLine($"✅ {summary.Title}: {summary.Reports.Length} benchmarks completed");
            if (summary.HasCriticalValidationErrors)
            {
                Console.WriteLine($"❌ Critical validation errors in {summary.Title}");
            }
        }
        
        Console.WriteLine();
        Console.WriteLine("🎯 Performance validation complete - check results for regressions");
    }
}