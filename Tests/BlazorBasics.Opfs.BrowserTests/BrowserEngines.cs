namespace BlazorBasics.Opfs.BrowserTests;

/// <summary>
/// The engines to run in. Playwright's WebKit for Windows accepts writes and keeps nothing, so WebKit needs Linux:
/// see <c>Tests/run-webkit-tests.ps1</c>. <c>OPFS_BROWSERS</c> overrides the list, separated by commas.
/// </summary>
internal static class BrowserEngines
{
    private const string OVERRIDE_VARIABLE = "OPFS_BROWSERS";

    public static IReadOnlyList<string> Names
    {
        get
        {
            string? chosen = Environment.GetEnvironmentVariable(OVERRIDE_VARIABLE);
            string[] names = OperatingSystem.IsWindows() ? ["chromium", "firefox"] : ["chromium", "firefox", "webkit"];
            return string.IsNullOrWhiteSpace(chosen) ? names : chosen.Split(',', StringSplitOptions.TrimEntries);
        }
    }
}
