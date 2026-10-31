namespace BlazorBasics.Opfs.Interop;

/// <summary>The reading functions of <c>opfs.js</c>.</summary>
[SupportedOSPlatform("browser")]
internal static partial class OpfsFileInterop
{
    [JSImport("fileInfoOrNull", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Promise<JSType.String>>]
    public static partial Task<string?> FileInfoOrNullAsync([JSMarshalAs<JSType.Array<JSType.String>>] string[] names);

    [JSImport("snapshotOrNull", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Promise<JSType.Object>>]
    public static partial Task<JSObject?> SnapshotOrNullAsync([JSMarshalAs<JSType.Array<JSType.String>>] string[] names);

    [JSImport("listFolder", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Promise<JSType.String>>]
    public static partial Task<string> ListFolderAsync([JSMarshalAs<JSType.Array<JSType.String>>] string[] names, string? textSuffix);

    [JSImport("listTree", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Promise<JSType.String>>]
    public static partial Task<string> ListTreeAsync([JSMarshalAs<JSType.Array<JSType.String>>] string[] names, string? textSuffix);

    [JSImport("readText", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Promise<JSType.String>>]
    public static partial Task<string> ReadTextAsync([JSMarshalAs<JSType.Array<JSType.String>>] string[] names);

    [JSImport("readBytes", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Promise<JSType.Object>>]
    public static partial Task<JSObject> ReadBytesAsync([JSMarshalAs<JSType.Array<JSType.String>>] string[] names);

    [JSImport("readRange", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Promise<JSType.Object>>]
    public static partial Task<JSObject> ReadRangeAsync([JSMarshalAs<JSType.Array<JSType.String>>] string[] names, double start, double end);

    [JSImport("readSnapshotRange", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Promise<JSType.Object>>]
    public static partial Task<JSObject> ReadSnapshotRangeAsync(JSObject file, double start, double end);

    [JSImport("lengthOfSnapshot", OpfsModule.MODULE_NAME)]
    public static partial double LengthOfSnapshot(JSObject file);

    [JSImport("bytesOf", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Array<JSType.Number>>]
    public static partial byte[] BytesOf(JSObject bytes);
}
