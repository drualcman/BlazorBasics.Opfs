namespace BlazorBasics.Opfs.Interop;

/// <summary>The JavaScript module of this package, imported once, the first time it is needed.</summary>
[SupportedOSPlatform("browser")]
internal static class OpfsModule
{
    public const string MODULE_NAME = "BlazorBasics.Opfs";

    private const string MODULE_URL = "../_content/BlazorBasics.Opfs/opfs.js";

    private static readonly Lazy<Task> Import = new(() => JSHost.ImportAsync(MODULE_NAME, MODULE_URL));

    public static Task ImportedAsync() => Import.Value;
}
