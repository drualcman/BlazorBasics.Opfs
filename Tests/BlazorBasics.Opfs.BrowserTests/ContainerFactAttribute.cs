namespace BlazorBasics.Opfs.BrowserTests;

/// <summary>A test for <see cref="ContainerTheoryAttribute"/> of what one engine does alone, skipped in a run without that engine.</summary>
public sealed class ContainerFactAttribute : FactAttribute
{
    public ContainerFactAttribute(string engine)
    {
        if (ContainerTheoryAttribute.DiskFolder is null || !BrowserEngines.Names.Contains(engine))
        {
            Skip = $"This run is not in the container, or it has no {engine}.";
        }
    }
}
