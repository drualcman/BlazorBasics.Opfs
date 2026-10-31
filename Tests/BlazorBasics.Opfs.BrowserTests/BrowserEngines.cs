namespace BlazorBasics.Opfs.BrowserTests;

/// <summary>
/// The engines to run in. Playwright's WebKit for Windows accepts writes and keeps nothing, so WebKit needs Linux:
/// see <c>Tests/run-webkit-tests.ps1</c>. <c>OPFS_BROWSERS</c> overrides the list, separated by commas.
/// </summary>
internal static class BrowserEngines
{
    public const string CHROMIUM = "chromium";
    public const string FIREFOX = "firefox";
    public const string WEBKIT = "webkit";

    private const string OVERRIDE_VARIABLE = "OPFS_BROWSERS";

    public static IReadOnlyList<string> Names
    {
        get
        {
            string? chosen = Environment.GetEnvironmentVariable(OVERRIDE_VARIABLE);
            string[] names = OperatingSystem.IsWindows() ? [CHROMIUM, FIREFOX] : [CHROMIUM, FIREFOX, WEBKIT];
            return string.IsNullOrWhiteSpace(chosen) ? names : chosen.Split(',', StringSplitOptions.TrimEntries);
        }
    }
}
