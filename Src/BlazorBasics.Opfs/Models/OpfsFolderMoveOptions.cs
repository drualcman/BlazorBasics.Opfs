namespace BlazorBasics.Opfs.Models;

/// <summary>The names a folder move leaves behind while it runs, so that an application keeps its own.</summary>
public sealed record OpfsFolderMoveOptions
{
    /// <summary>The folder under the root that holds the notes of unfinished moves.</summary>
    public string NotesFolderName { get; init; } = ".opfs-moves";

    /// <summary>What the name of the Web Lock over a root's moves starts with.</summary>
    public string LockPrefix { get; init; } = "BlazorBasics.Opfs-moves:";
}
