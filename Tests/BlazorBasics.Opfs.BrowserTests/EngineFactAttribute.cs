namespace BlazorBasics.Opfs.BrowserTests;

/// <summary>A test of what one engine does alone, skipped in a run without that engine.</summary>
public sealed class EngineFactAttribute : FactAttribute
{
    public EngineFactAttribute(string engine)
    {
        if (!BrowserEngines.Names.Contains(engine))
        {
            Skip = $"This run has no {engine}.";
        }
    }
}
