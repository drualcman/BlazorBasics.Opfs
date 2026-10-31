namespace BlazorBasics.Opfs.Interop;

/// <summary>The functions of <c>opfs.js</c> about the browser rather than about a file: probe, locks and storage.</summary>
[SupportedOSPlatform("browser")]
internal static partial class OpfsCoordinationInterop
{
    [JSImport("probe", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Promise<JSType.String>>]
    public static partial Task<string> ProbeAsync();

    [JSImport("tryAcquireLock", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Promise<JSType.Object>>]
    public static partial Task<JSObject?> TryAcquireLockAsync(string name, string mode);

    [JSImport("releaseLock", OpfsModule.MODULE_NAME)]
    public static partial void ReleaseLock(JSObject holder);

    [JSImport("estimate", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Promise<JSType.String>>]
    public static partial Task<string?> EstimateAsync();

    [JSImport("isPersisted", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Promise<JSType.Boolean>>]
    public static partial Task<bool> IsPersistedAsync();

    [JSImport("requestPersistence", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Promise<JSType.Boolean>>]
    public static partial Task<bool> RequestPersistenceAsync();
}
