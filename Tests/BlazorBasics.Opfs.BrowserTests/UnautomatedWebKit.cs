using System.Diagnostics;

namespace BlazorBasics.Opfs.BrowserTests;

/// <summary>
/// Playwright's WebKit opened on a display with no automation, because under Playwright it never enters the back/forward
/// cache. It only exists in the container of <c>Tests/run-webkit-tests.ps1</c>, which installs the display.
/// </summary>
internal static class UnautomatedWebKit
{
    private const string BROWSERS_VARIABLE = "PLAYWRIGHT_BROWSERS_PATH";

    public static Process Open(string address)
    {
        string browsers = Environment.GetEnvironmentVariable(BROWSERS_VARIABLE)
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache", "ms-playwright");
        string launcher = Path.Combine(Directory.GetDirectories(browsers, "webkit-*").Single(), "pw_run.sh");
        ProcessStartInfo start = new("xvfb-run", ["-a", "dbus-launch", "--exit-with-session", launcher, address]);
        start.Environment["WEBKIT_DISABLE_SANDBOX_THIS_IS_DANGEROUS"] = "1";
        start.Environment["WEBKIT_DISABLE_COMPOSITING_MODE"] = "1";
        start.Environment["LIBGL_ALWAYS_SOFTWARE"] = "1";
        return Process.Start(start)!;
    }
}
