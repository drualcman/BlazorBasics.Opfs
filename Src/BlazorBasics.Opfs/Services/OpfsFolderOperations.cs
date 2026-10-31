namespace BlazorBasics.Opfs.Services;

/// <summary>Removes entries and moves whole folders.</summary>
[SupportedOSPlatform("browser")]
internal static class OpfsFolderOperations
{
    public static Task DeleteAsync(OpfsPath fileOrFolder, CancellationToken cancellationToken) =>
        OpfsCall.RunAsync(() => OpfsFolderInterop.RemoveAsync(OpfsContent.FolderNamesOf(fileOrFolder)), cancellationToken);

    public static Task ClearFolderAsync(OpfsPath folder, CancellationToken cancellationToken) =>
        OpfsCall.RunAsync(() => OpfsFolderInterop.ClearFolderAsync(OpfsContent.FolderNamesOf(folder)), cancellationToken);

    public static Task MoveFolderAsync(OpfsPath root, OpfsPath source, OpfsPath destination, OpfsFolderMoveOptions options, CancellationToken cancellationToken) =>
        OpfsCall.RunAsync(() => OpfsFolderInterop.MoveFolderAsync(
            OpfsContent.FolderNamesOf(root), OpfsContent.NamesOf(source), OpfsContent.NamesOf(destination), options.NotesFolderName, options.LockPrefix)
            .WaitAsync(cancellationToken), cancellationToken);

    public static Task FinishPendingFolderMovesAsync(OpfsPath root, OpfsFolderMoveOptions options, CancellationToken cancellationToken) =>
        OpfsCall.RunAsync(() => OpfsFolderInterop.FinishPendingMovesAsync(OpfsContent.FolderNamesOf(root), options.NotesFolderName, options.LockPrefix)
            .WaitAsync(cancellationToken), cancellationToken);
}
