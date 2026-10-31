namespace BlazorBasics.Opfs.Interop;

/// <summary>The functions of <c>opfs.js</c> that move data with no bytes in .NET.</summary>
[SupportedOSPlatform("browser")]
internal static partial class OpfsTransferInterop
{
    [JSImport("copy", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Promise<JSType.Number>>]
    public static partial Task<double> CopyAsync(int operationId, [JSMarshalAs<JSType.Array<JSType.String>>] string[] from, [JSMarshalAs<JSType.Array<JSType.String>>] string[] to);

    [JSImport("moveFile", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Promise<JSType.Void>>]
    public static partial Task MoveFileAsync([JSMarshalAs<JSType.Array<JSType.String>>] string[] from, [JSMarshalAs<JSType.Array<JSType.String>>] string[] to);

    [JSImport("fetchTo", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Promise<JSType.String>>]
    public static partial Task<string> FetchToAsync(int operationId, string url, [JSMarshalAs<JSType.Array<JSType.String>>] string[] names, double maxBytes, string mode, string cache, string credentials);

    [JSImport("abortOperation", OpfsModule.MODULE_NAME)]
    public static partial void AbortOperation(int operationId);

    [JSImport("objectUrl", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Promise<JSType.String>>]
    public static partial Task<string> ObjectUrlAsync([JSMarshalAs<JSType.Array<JSType.String>>] string[] names);

    [JSImport("revokeObjectUrl", OpfsModule.MODULE_NAME)]
    public static partial void RevokeObjectUrl(string url);

    [JSImport("downloadFile", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Promise<JSType.Void>>]
    public static partial Task DownloadFileAsync([JSMarshalAs<JSType.Array<JSType.String>>] string[] names, string suggestedFileName);
}
