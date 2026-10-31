namespace BlazorBasics.Opfs.Testing;

/// <summary>
/// A file system in memory that passes the same contract battery as the browser's. A stale snapshot fails, as in Chromium and
/// Firefox, and a file with an open writer refuses to move. Downloads go through <see cref="Fetcher"/>. Use it from one
/// test at a time.
/// </summary>
public sealed partial class InMemoryOpfsFileSystem : IOpfsFileSystem
{
    private readonly InMemoryStore Store = new();

    /// <summary>Answers the requests of <see cref="FetchToAsync"/>. With none set, every download fails as a network failure would.</summary>
    public Func<Uri, CancellationToken, Task<HttpResponseMessage>>? Fetcher { get; set; }

    /// <summary>What the next probe answers.</summary>
    public OpfsProbeResult ProbeResult { get; set; } = new(true, string.Empty);

    /// <inheritdoc />
    public Task<OpfsProbeResult> ProbeAsync(CancellationToken cancellationToken = default) => Task.FromResult(ProbeResult);

    /// <inheritdoc />
    public Task<OpfsFileInfo?> GetInfoOrNullAsync(OpfsPath file, CancellationToken cancellationToken = default)
    {
        InMemoryFile? found = Store.FileOrNull(file);
        return Task.FromResult(found is null ? null : InfoOf(file.Name, found, null));
    }

    /// <inheritdoc />
    public Task<IOpfsFileSnapshot?> OpenSnapshotOrNullAsync(OpfsPath file, CancellationToken cancellationToken = default)
    {
        InMemoryFile? found = Store.FileOrNull(file);
        IOpfsFileSnapshot? snapshot = found is null ? null : new InMemoryFileSnapshot(found);
        return Task.FromResult(snapshot);
    }

    /// <inheritdoc />
    public Task<OpfsFolderContent> ListAsync(OpfsPath folder, string? textSuffix = null, CancellationToken cancellationToken = default)
    {
        Store.EnsureFolder(folder, false);
        List<OpfsFileInfo> files = Store.Files.Where(file => file.Key.Parent == folder)
            .Select(file => InfoOf(file.Key.Name, file.Value, textSuffix)).OrderBy(file => file.Name, StringComparer.Ordinal).ToList();
        List<string> folders = Store.Folders.Where(found => !found.IsRoot && found.Parent == folder)
            .Select(found => found.Name).Order(StringComparer.Ordinal).ToList();
        return Task.FromResult(new OpfsFolderContent(files, folders));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<OpfsFileInfo>> ListTreeAsync(OpfsPath folder, string? textSuffix = null, CancellationToken cancellationToken = default)
    {
        Store.EnsureFolder(folder, false);
        int prefixLength = folder.IsRoot ? 0 : folder.ToString().Length + 1;
        IReadOnlyList<OpfsFileInfo> files = Store.Files.Where(file => Store.IsUnder(file.Key, folder))
            .Select(file => InfoOf(file.Key.ToString()[prefixLength..], file.Value, textSuffix)).OrderBy(file => file.Name, StringComparer.Ordinal).ToList();
        return Task.FromResult(files);
    }

    /// <inheritdoc />
    public Task<string> ReadTextAsync(OpfsPath file, CancellationToken cancellationToken = default) =>
        Task.FromResult(System.Text.Encoding.UTF8.GetString(Store.RequireFile(file).Content));

    /// <inheritdoc />
    public Task<byte[]> ReadBytesAsync(OpfsPath file, CancellationToken cancellationToken = default) =>
        Task.FromResult(Store.RequireFile(file).Content.ToArray());

    /// <inheritdoc />
    public Task<byte[]> ReadRangeAsync(OpfsPath file, long start, int length, CancellationToken cancellationToken = default) =>
        Task.FromResult(InMemoryFileSnapshot.Slice(Store.RequireFile(file).Content, start, length));

    /// <inheritdoc />
    public Stream OpenLazyRead(OpfsPath file, long length) => new OpfsLazyReadStream(this, file, length);

    private static OpfsFileInfo InfoOf(string name, InMemoryFile file, string? textSuffix)
    {
        string? text = !string.IsNullOrEmpty(textSuffix) && name.EndsWith(textSuffix, StringComparison.Ordinal)
            ? System.Text.Encoding.UTF8.GetString(file.Content)
            : null;
        return new OpfsFileInfo(name, file.Content.Length, file.ModifiedUtc, text);
    }
}
