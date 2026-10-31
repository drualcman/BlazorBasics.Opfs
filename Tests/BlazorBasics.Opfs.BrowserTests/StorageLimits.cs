namespace BlazorBasics.Opfs.BrowserTests;

/// <summary>
/// How much one origin can store in each engine under test. Firefox takes it from a preference that only works in the
/// <c>user.js</c> of the profile, Chromium from the DevTools protocol, and WebKit has no way to set it: it is always 1000 MiB.
/// </summary>
internal static class StorageLimits
{
    public const string FIREFOX_USER_FILE = "user.js";
    public const string FIREFOX_USER_PREFERENCE = "user_pref(\"dom.quotaManager.temporaryStorage.fixedLimit\", 65536);\n";
    public const int CHROMIUM_QUOTA_BYTES = 16 * 1024 * 1024;

    public static int MebibytesOf(string engine) => engine switch
    {
        BrowserEngines.CHROMIUM => 16,
        BrowserEngines.FIREFOX => 12,
        _ => 1000
    };
}
