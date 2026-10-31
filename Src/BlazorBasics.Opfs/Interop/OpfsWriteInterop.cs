namespace BlazorBasics.Opfs.Interop;

/// <summary>The writing functions of <c>opfs.js</c>. The bytes cross as a view that JavaScript copies at once.</summary>
[SupportedOSPlatform("browser")]
internal static partial class OpfsWriteInterop
{
    [JSImport("writeBytes", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Promise<JSType.Void>>]
    public static partial Task WriteBytesAsync([JSMarshalAs<JSType.Array<JSType.String>>] string[] names, [JSMarshalAs<JSType.MemoryView>] ArraySegment<byte> content);

    [JSImport("writeText", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Promise<JSType.Void>>]
    public static partial Task WriteTextAsync([JSMarshalAs<JSType.Array<JSType.String>>] string[] names, string content);

    [JSImport("appendBytes", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Promise<JSType.Void>>]
    public static partial Task AppendBytesAsync([JSMarshalAs<JSType.Array<JSType.String>>] string[] names, [JSMarshalAs<JSType.MemoryView>] ArraySegment<byte> content);

    [JSImport("truncate", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Promise<JSType.Void>>]
    public static partial Task TruncateAsync([JSMarshalAs<JSType.Array<JSType.String>>] string[] names, double length);

    [JSImport("openWriter", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Promise<JSType.Object>>]
    public static partial Task<JSObject> OpenWriterAsync([JSMarshalAs<JSType.Array<JSType.String>>] string[] names, bool keepExistingData);

    [JSImport("writeToWriter", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Promise<JSType.Void>>]
    public static partial Task WriteToWriterAsync(JSObject writer, [JSMarshalAs<JSType.MemoryView>] ArraySegment<byte> content);

    [JSImport("completeWriter", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Promise<JSType.Void>>]
    public static partial Task CompleteWriterAsync(JSObject writer);

    [JSImport("abortWriter", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Promise<JSType.Void>>]
    public static partial Task AbortWriterAsync(JSObject writer);

    [JSImport("abandonWriter", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Promise<JSType.Void>>]
    public static partial Task AbandonWriterAsync(JSObject writer);
}
