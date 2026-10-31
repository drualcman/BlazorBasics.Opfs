namespace BlazorBasics.Opfs.BrowserTests;

/// <summary>A test that needs the DevTools protocol, so it is skipped in a run without Chromium.</summary>
public sealed class ChromiumFactAttribute : FactAttribute
{
    public ChromiumFactAttribute()
    {
        if (!BrowserEngines.Names.Contains("chromium"))
        {
            Skip = "This run has no Chromium.";
        }
    }
}
