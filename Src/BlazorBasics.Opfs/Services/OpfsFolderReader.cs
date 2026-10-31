namespace BlazorBasics.Opfs.Services;

/// <summary>Lists folders, in ordinal order whatever order the browser gave.</summary>
[SupportedOSPlatform("browser")]
internal static class OpfsFolderReader
{
    public static Task<OpfsFolderContent> ListAsync(OpfsPath folder, string? textSuffix, CancellationToken cancellationToken) =>
        OpfsCall.RunAsync(async () =>
        {
            string json = await OpfsFileInterop.ListFolderAsync(OpfsContent.FolderNamesOf(folder), textSuffix);
            OpfsFolderRecord content = JsonSerializer.Deserialize(json, OpfsJsonContext.Default.OpfsFolderRecord)!;
            return new OpfsFolderContent(InfoOf(content.Files), content.Folders.Order(StringComparer.Ordinal).ToList());
        }, cancellationToken);

    public static Task<IReadOnlyList<OpfsFileInfo>> ListTreeAsync(OpfsPath folder, string? textSuffix, CancellationToken cancellationToken) =>
        OpfsCall.RunAsync(async () =>
        {
            string json = await OpfsFileInterop.ListTreeAsync(OpfsContent.FolderNamesOf(folder), textSuffix);
            return InfoOf(JsonSerializer.Deserialize(json, OpfsJsonContext.Default.OpfsFileRecordArray)!);
        }, cancellationToken);

    private static IReadOnlyList<OpfsFileInfo> InfoOf(OpfsFileRecord[] files) =>
        files.Select(file => file.ToInfo()).OrderBy(file => file.Name, StringComparer.Ordinal).ToList();
}
