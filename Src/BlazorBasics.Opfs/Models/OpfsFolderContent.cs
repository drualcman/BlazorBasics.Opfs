namespace BlazorBasics.Opfs.Models;

/// <summary>What a folder holds, sorted by ordinal name.</summary>
/// <param name="Files">The files in the folder.</param>
/// <param name="Folders">The stored names of the folders in the folder.</param>
public sealed record OpfsFolderContent(IReadOnlyList<OpfsFileInfo> Files, IReadOnlyList<string> Folders);
