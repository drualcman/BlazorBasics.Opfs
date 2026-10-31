namespace BlazorBasics.Opfs.Interop;

/// <summary>The functions of <c>opfs.js</c> that remove and move whole entries.</summary>
[SupportedOSPlatform("browser")]
internal static partial class OpfsFolderInterop
{
    [JSImport("remove", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Promise<JSType.Void>>]
    public static partial Task RemoveAsync([JSMarshalAs<JSType.Array<JSType.String>>] string[] names);

    [JSImport("clearFolder", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Promise<JSType.Void>>]
    public static partial Task ClearFolderAsync([JSMarshalAs<JSType.Array<JSType.String>>] string[] names);

    [JSImport("moveFolder", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Promise<JSType.Void>>]
    public static partial Task MoveFolderAsync(
        [JSMarshalAs<JSType.Array<JSType.String>>] string[] rootNames,
        [JSMarshalAs<JSType.Array<JSType.String>>] string[] sourceNames,
        [JSMarshalAs<JSType.Array<JSType.String>>] string[] destinationNames,
        string notesFolderName,
        string lockPrefix);

    [JSImport("finishPendingMoves", OpfsModule.MODULE_NAME)]
    [return: JSMarshalAs<JSType.Promise<JSType.Void>>]
    public static partial Task FinishPendingMovesAsync([JSMarshalAs<JSType.Array<JSType.String>>] string[] rootNames, string notesFolderName, string lockPrefix);
}
