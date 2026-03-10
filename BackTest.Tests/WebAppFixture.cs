using Microsoft.AspNetCore.Mvc.Testing;

namespace BackTest.Tests;

/// <summary>
/// A single <see cref="WebApplicationFactory{TEntryPoint}"/> shared across all
/// controller component-test classes via xUnit's collection fixture mechanism.
/// This ensures the in-memory database is only seeded once, preventing
/// duplicate-key errors when multiple test classes boot the application.
/// </summary>
public class WebAppFixture : WebApplicationFactory<Program> { }

[CollectionDefinition(SharedCollection.Name)]
public class SharedCollection : ICollectionFixture<WebAppFixture>
{
    public const string Name = "Shared WebApp Collection";
}
