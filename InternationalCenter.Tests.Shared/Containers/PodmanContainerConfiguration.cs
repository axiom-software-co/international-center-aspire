using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace InternationalCenter.Tests.Shared.Containers;

/// <summary>
/// Configures TestContainers to use Podman instead of Docker for NixOS compatibility
/// Following Microsoft recommended patterns for test infrastructure
/// </summary>
public static class PodmanContainerConfiguration
{
    static PodmanContainerConfiguration()
    {
        // Configure TestContainers 4.0+ to use Podman on NixOS
        TestcontainersSettings.ResourceReaperEnabled = false;
        
        // Primary Podman socket configuration
        var podmanSocket = "/run/user/1000/podman/podman.sock";
        Environment.SetEnvironmentVariable("DOCKER_HOST", $"unix://{podmanSocket}");
        Environment.SetEnvironmentVariable("TESTCONTAINERS_RYUK_DISABLED", "true");
        
        // TestContainers 4.0+ specific settings
        Environment.SetEnvironmentVariable("TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE", podmanSocket);
        Environment.SetEnvironmentVariable("TC_HOST", "unix://" + podmanSocket);
    }

    public static PostgreSqlBuilder CreatePostgreSqlContainer() =>
        new PostgreSqlBuilder()
            .WithDatabase("test_international_center")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .WithPortBinding(0, 5432) // Random host port
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilPortIsAvailable(5432))
            .WithCleanUp(true);

    public static RedisBuilder CreateGarnetContainer() =>
        new RedisBuilder()
            .WithImage("ghcr.io/microsoft/garnet")
            .WithPortBinding(0, 6379) // Random host port
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilPortIsAvailable(6379))
            .WithCleanUp(true);

    /// <summary>
    /// Validates that Podman is available and containers can be created
    /// </summary>
    public static async Task<bool> ValidateContainerRuntimeAsync()
    {
        try
        {
            var testContainer = new ContainerBuilder()
                .WithImage("hello-world")
                .WithWaitStrategy(Wait.ForUnixContainer()
                    .UntilCommandIsCompleted("echo", "test"))
                .Build();

            await testContainer.StartAsync();
            await testContainer.StopAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}