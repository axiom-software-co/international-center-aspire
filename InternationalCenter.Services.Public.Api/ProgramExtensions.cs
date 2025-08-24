namespace InternationalCenter.Services.Public.Api;

/// <summary>
/// Partial Program class to make the implicit Program class accessible for testing
/// This enables WebApplicationFactory<Program> in integration tests
/// </summary>
public partial class Program
{
    // This partial class declaration makes the implicit Program class public
    // Required for ASP.NET Core integration testing with WebApplicationFactory
}