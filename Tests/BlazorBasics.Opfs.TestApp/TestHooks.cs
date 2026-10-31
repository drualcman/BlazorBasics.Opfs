using System.Runtime.InteropServices.JavaScript;

namespace BlazorBasics.Opfs.TestApp;

/// <summary>What the tests ask of the page or worker that hosts them, in <c>test-hooks.js</c>.</summary>
internal static partial class TestHooks
{
    [JSImport("globalThis.opfsTestHooks.disableMove")]
    public static partial void DisableMove();

    [JSImport("globalThis.opfsTestHooks.enableMove")]
    public static partial void EnableMove();

    [JSImport("globalThis.opfsTestHooks.fetchText")]
    [return: JSMarshalAs<JSType.Promise<JSType.String>>]
    public static partial Task<string> FetchTextAsync(string url);

    [JSImport("globalThis.opfsTestHooks.heapBytes")]
    public static partial double HeapBytes();

    [JSImport("globalThis.opfsTestHooks.inWorker")]
    public static partial bool InWorker();

    [JSImport("globalThis.opfsTestHooks.queryValue")]
    public static partial string? QueryValue(string name);

    [JSImport("globalThis.opfsTestHooks.reportProgress")]
    public static partial void ReportProgress(string testName);
}
