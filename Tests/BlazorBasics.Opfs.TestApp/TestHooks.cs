using System.Runtime.InteropServices.JavaScript;

namespace BlazorBasics.Opfs.TestApp;

/// <summary>What the tests ask of the page itself, in <c>main.js</c>.</summary>
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
}
