using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using InternationalCenter.Services.Admin.Api.Benchmarks.Benchmarks;

namespace InternationalCenter.Services.Admin.Api.Benchmarks;

/// <summary>
/// BenchmarkDotNet runner for Services.Admin.Api medical-grade performance validation
/// WHY: Medical-grade API performance benchmarks required for compliance and reliability
/// SCOPE: Admin API with EF Core repository patterns and medical-grade audit requirements
/// CONTEXT: Admin gateway architecture with Entra External ID requires performance validation
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("🏥 Services.Admin.Api Medical-Grade Performance Benchmarks");
        Console.WriteLine("========================================================");
        Console.WriteLine("WHY: Medical-grade APIs require performance validation for compliance");
        Console.WriteLine("SCOPE: EF Core repositories, use cases, and admin operations");
        Console.WriteLine("CONTEXT: Admin gateway with Entra External ID authentication");
        Console.WriteLine();

        // Configure BenchmarkDotNet for medical-grade testing
        var config = DefaultConfig.Instance
            .AddJob(Job.Default
                .WithRuntime(CoreRuntime.Net90)
                .WithPlatform(Platform.X64)
                .WithToolchain(InProcessEmitToolchain.Instance)) // Faster execution for CI/CD
            .WithOptions(ConfigOptions.DisableLogFile); // Reduce output for cleaner CI logs

        // Run all medical-grade benchmark classes
        var benchmarkSummaries = new[]
        {
            BenchmarkRunner.Run<EfCoreRepositoryBenchmarks>(config),
            BenchmarkRunner.Run<AdminUseCaseBenchmarks>(config),
            BenchmarkRunner.Run<AdminApiEndpointBenchmarks>(config),
            BenchmarkRunner.Run<MedicalGradeAuditBenchmarks>(config)
        };

        Console.WriteLine();
        Console.WriteLine("🏥 Medical-Grade Benchmark Summary:");
        foreach (var summary in benchmarkSummaries)
        {
            Console.WriteLine($"✅ {summary.Title}: {summary.Reports.Length} benchmarks completed");
            if (summary.HasCriticalValidationErrors)
            {
                Console.WriteLine($"❌ Critical validation errors in {summary.Title}");
            }
        }
        
        Console.WriteLine();
        Console.WriteLine("🎯 Medical-grade performance validation complete - check results for compliance");
    }
}