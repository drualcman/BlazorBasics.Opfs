namespace BlazorBasics.Opfs.Abstractions;

/// <summary>
/// The Origin Private File System by path. Failures are typed <see cref="OpfsException"/>s and are never hidden, except
/// where a member says so. Every write is all or nothing: the browser swaps the file in when the write closes.
/// </summary>
public interface IOpfsFileSystem
{
    /// <summary>Answers whether this browser can really store files here. It never throws: the failure comes in the result.</summary>
    Task<OpfsProbeResult> ProbeAsync(CancellationToken cancellationToken = default);

    /// <summary>Describes a file, or returns null only when it does not exist. Any other failure throws.</summary>
    Task<OpfsFileInfo?> GetInfoOrNullAsync(OpfsPath file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Takes a snapshot to read a file through, or returns null only when it does not exist. A later write to the file
    /// invalidates the snapshot, and what happens then depends on the browser: see <see cref="IOpfsFileSnapshot"/>.
    /// </summary>
    Task<IOpfsFileSnapshot?> OpenSnapshotOrNullAsync(OpfsPath file, CancellationToken cancellationToken = default);

    /// <summary>Lists a folder, sorted by ordinal name. A missing folder throws <see cref="OpfsNotFoundException"/>.</summary>
    /// <param name="folder">The folder to list.</param>
    /// <param name="textSuffix">The files whose name ends in this come with their text already read.</param>
    /// <param name="cancellationToken">Stops waiting for the listing.</param>
    Task<OpfsFolderContent> ListAsync(OpfsPath folder, string? textSuffix = null, CancellationToken cancellationToken = default);

    /// <summary>Lists every file under a folder at any depth, with its path relative to the folder as its name.</summary>
    /// <param name="folder">The folder to walk.</param>
    /// <param name="textSuffix">The files whose name ends in this come with their text already read.</param>
    /// <param name="cancellationToken">Stops waiting for the listing.</param>
    Task<IReadOnlyList<OpfsFileInfo>> ListTreeAsync(OpfsPath folder, string? textSuffix = null, CancellationToken cancellationToken = default);

    /// <summary>Reads a whole file as UTF-8 text.</summary>
    Task<string> ReadTextAsync(OpfsPath file, CancellationToken cancellationToken = default);

    /// <summary>Reads a whole file into WebAssembly memory. Only for small files.</summary>
    Task<byte[]> ReadBytesAsync(OpfsPath file, CancellationToken cancellationToken = default);

    /// <summary>Reads part of a file. A range past the end comes back shorter.</summary>
    Task<byte[]> ReadRangeAsync(OpfsPath file, long start, int length, CancellationToken cancellationToken = default);

    /// <summary>Gives a stream that reads nothing until it is read, a slice at a time. It is asynchronous only.</summary>
    Stream OpenLazyRead(OpfsPath file, long length);

    /// <summary>Replaces a file with these bytes, creating it and its folders when needed.</summary>
    Task WriteBytesAsync(OpfsPath file, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default);

    /// <summary>Replaces a file with this text in UTF-8, creating it and its folders when needed.</summary>
    Task WriteTextAsync(OpfsPath file, string content, CancellationToken cancellationToken = default);

    /// <summary>Replaces a file with a stream. If the stream fails, the file stays as it was.</summary>
    Task WriteStreamAsync(OpfsPath file, Stream content, CancellationToken cancellationToken = default);

    /// <summary>Opens a write that stays open across several chunks. Nothing is visible until it completes.</summary>
    /// <param name="file">The file to write.</param>
    /// <param name="keepExistingData">Starts from the current content, positioned at its end. The browser copies the whole file to do so.</param>
    /// <param name="cancellationToken">Stops waiting for the writer.</param>
    Task<IOpfsWriter> OpenWriterAsync(OpfsPath file, bool keepExistingData = false, CancellationToken cancellationToken = default);

    /// <summary>Adds to the end of a file. The browser copies the whole file: use a writer for a series of chunks.</summary>
    Task AppendAsync(OpfsPath file, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default);

    /// <summary>Cuts a file to this length. A file already that short or shorter stays as it is.</summary>
    Task TruncateAsync(OpfsPath file, long length, CancellationToken cancellationToken = default);

    /// <summary>Copies a file inside the browser, with no bytes in .NET, and returns how many it copied.</summary>
    Task<long> CopyAsync(OpfsPath from, OpfsPath to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a file over any destination, which is never removed first. A file with an open writer may refuse with
    /// <see cref="OpfsFileBusyException"/>, depending on the browser.
    /// </summary>
    Task MoveFileAsync(OpfsPath from, OpfsPath to, CancellationToken cancellationToken = default);

    /// <summary>Downloads a URL straight to a file with no bytes in .NET. A cut download leaves the file as it was.</summary>
    Task<OpfsFetchResult> FetchToAsync(Uri url, OpfsPath file, OpfsFetchOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Creates a <c>blob:</c> address for a file. Give it back with <see cref="RevokeObjectUrl"/>.</summary>
    Task<string> CreateObjectUrlAsync(OpfsPath file, CancellationToken cancellationToken = default);

    /// <summary>Frees an address made by <see cref="CreateObjectUrlAsync"/>.</summary>
    void RevokeObjectUrl(string url);

    /// <summary>
    /// Makes the browser save a file to the user's downloads. It needs a page: in a worker it throws
    /// <see cref="Exceptions.OpfsNotSupportedException"/>.
    /// </summary>
    Task DownloadAsync(OpfsPath file, string suggestedFileName, CancellationToken cancellationToken = default);

    /// <summary>Removes a file or a whole folder. What is not there counts as removed. The root is emptied, not removed.</summary>
    Task DeleteAsync(OpfsPath fileOrFolder, CancellationToken cancellationToken = default);

    /// <summary>Removes everything inside a folder and keeps the folder. A missing folder counts as empty.</summary>
    Task ClearFolderAsync(OpfsPath folder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a whole folder under a lock, leaving a note first, so that a move a closed tab left halfway can be finished by
    /// <see cref="FinishPendingFolderMovesAsync"/>. A missing source does nothing.
    /// </summary>
    /// <param name="root">The folder that holds the notes; source and destination are relative to it.</param>
    /// <param name="source">The folder to move.</param>
    /// <param name="destination">Where it ends up.</param>
    /// <param name="options">The names of the notes folder and the lock.</param>
    /// <param name="cancellationToken">Stops waiting. The move itself goes on, so that it never stays halfway.</param>
    Task MoveFolderAsync(OpfsPath root, OpfsPath source, OpfsPath destination, OpfsFolderMoveOptions options, CancellationToken cancellationToken = default);

    /// <summary>Finishes every folder move that was left halfway under a root.</summary>
    Task FinishPendingFolderMovesAsync(OpfsPath root, OpfsFolderMoveOptions options, CancellationToken cancellationToken = default);
}
