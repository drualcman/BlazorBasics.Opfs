namespace BlazorBasics.Opfs.BrowserTests;

/// <summary>Everything that opens a browser shares one fixture and runs in sequence.</summary>
[CollectionDefinition(NAME)]
public sealed class BrowserCollection : ICollectionFixture<BrowserFixture>
{
    public const string NAME = "Browser";
}
