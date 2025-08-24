using Xunit;

namespace InternationalCenter.Tests.Shared.TestCollections;

/// <summary>
/// Test collection definitions for controlling Aspire test execution sequencing.
/// 
/// Collections ensure that resource-intensive Aspire orchestration tests run sequentially
/// rather than in parallel, preventing resource conflicts and container port collisions.
/// </summary>

[CollectionDefinition("AspireInfrastructureTests", DisableParallelization = true)]
public class AspireInfrastructureTestCollection
{
    // This class has no code, and is never instantiated.
    // It exists only to define the collection for xUnit.
}

[CollectionDefinition("AspireApiTests", DisableParallelization = true)]
public class AspireApiTestCollection
{
    // This class has no code, and is never instantiated.
    // It exists only to define the collection for xUnit.
}

[CollectionDefinition("AspireEndToEndTests", DisableParallelization = true)]
public class AspireEndToEndTestCollection
{
    // This class has no code, and is never instantiated.
    // It exists only to define the collection for xUnit.
}