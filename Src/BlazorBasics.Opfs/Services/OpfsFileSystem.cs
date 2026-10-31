namespace BlazorBasics.Opfs.Services;

/// <summary>The file system of the browser. It needs no container, and outside a browser it throws <see cref="OpfsNotSupportedException"/>.</summary>
[SupportedOSPlatform("browser")]
public sealed class OpfsFileSystem : IOpfsFileSystem
{
    /// <inheritdoc />
    public Task<OpfsProbeResult> ProbeAsync(CancellationToken cancellationToken = default) => OpfsProbe.RunAsync(cancellationToken);

    /// <inheritdoc />
    public Task<OpfsFileInfo?> GetInfoOrNullAsync(OpfsPath file, CancellationToken cancellationToken = default) => OpfsFileReader.GetInfoOrNullAsync(file, cancellationToken);

    /// <inheritdoc />
    public Task<IOpfsFileSnapshot?> OpenSnapshotOrNullAsync(OpfsPath file, CancellationToken cancellationToken = default) => OpfsFileReader.OpenSnapshotOrNullAsync(file, cancellationToken);

    /// <inheritdoc />
    public Task<OpfsFolderContent> ListAsync(OpfsPath folder, string? textSuffix = null, CancellationToken cancellationToken = default) => OpfsFolderReader.ListAsync(folder, textSuffix, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<OpfsFileInfo>> ListTreeAsync(OpfsPath folder, string? textSuffix = null, CancellationToken cancellationToken = default) => OpfsFolderReader.ListTreeAsync(folder, textSuffix, cancellationToken);

    /// <inheritdoc />
    public Task<string> ReadTextAsync(OpfsPath file, CancellationToken cancellationToken = default) => OpfsFileReader.ReadTextAsync(file, cancellationToken);

    /// <inheritdoc />
    public Task<byte[]> ReadBytesAsync(OpfsPath file, CancellationToken cancellationToken = default) => OpfsFileReader.ReadBytesAsync(file, cancellationToken);

    /// <inheritdoc />
    public Task<byte[]> ReadRangeAsync(OpfsPath file, long start, int length, CancellationToken cancellationToken = default) => OpfsFileReader.ReadRangeAsync(file, start, length, cancellationToken);

    /// <inheritdoc />
    public Stream OpenLazyRead(OpfsPath file, long length) => new OpfsLazyReadStream(this, file, length);

    /// <inheritdoc />
    public Task WriteBytesAsync(OpfsPath file, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default) => OpfsFileWriter.WriteBytesAsync(file, content, cancellationToken);

    /// <inheritdoc />
    public Task WriteTextAsync(OpfsPath file, string content, CancellationToken cancellationToken = default) => OpfsFileWriter.WriteTextAsync(file, content, cancellationToken);

    /// <inheritdoc />
    public Task WriteStreamAsync(OpfsPath file, Stream content, CancellationToken cancellationToken = default) => OpfsFileWriter.WriteStreamAsync(file, content, cancellationToken);

    /// <inheritdoc />
    public Task<IOpfsWriter> OpenWriterAsync(OpfsPath file, bool keepExistingData = false, CancellationToken cancellationToken = default) => OpfsFileWriter.OpenWriterAsync(file, keepExistingData, cancellationToken);

    /// <inheritdoc />
    public Task AppendAsync(OpfsPath file, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default) => OpfsFileWriter.AppendAsync(file, content, cancellationToken);

    /// <inheritdoc />
    public Task TruncateAsync(OpfsPath file, long length, CancellationToken cancellationToken = default) => OpfsFileWriter.TruncateAsync(file, length, cancellationToken);

    /// <inheritdoc />
    public Task<long> CopyAsync(OpfsPath from, OpfsPath to, CancellationToken cancellationToken = default) => OpfsTransfers.CopyAsync(from, to, cancellationToken);

    /// <inheritdoc />
    public Task MoveFileAsync(OpfsPath from, OpfsPath to, CancellationToken cancellationToken = default) => OpfsTransfers.MoveFileAsync(from, to, cancellationToken);

    /// <inheritdoc />
    public Task<OpfsFetchResult> FetchToAsync(Uri url, OpfsPath file, OpfsFetchOptions? options = null, CancellationToken cancellationToken = default) => OpfsTransfers.FetchToAsync(url, file, options, cancellationToken);

    /// <inheritdoc />
    public Task<string> CreateObjectUrlAsync(OpfsPath file, CancellationToken cancellationToken = default) => OpfsTransfers.CreateObjectUrlAsync(file, cancellationToken);

    /// <inheritdoc />
    public void RevokeObjectUrl(string url) => OpfsTransfers.RevokeObjectUrl(url);

    /// <inheritdoc />
    public Task DownloadAsync(OpfsPath file, string suggestedFileName, CancellationToken cancellationToken = default) => OpfsTransfers.DownloadAsync(file, suggestedFileName, cancellationToken);

    /// <inheritdoc />
    public Task DeleteAsync(OpfsPath fileOrFolder, CancellationToken cancellationToken = default) => OpfsFolderOperations.DeleteAsync(fileOrFolder, cancellationToken);

    /// <inheritdoc />
    public Task ClearFolderAsync(OpfsPath folder, CancellationToken cancellationToken = default) => OpfsFolderOperations.ClearFolderAsync(folder, cancellationToken);

    /// <inheritdoc />
    public Task MoveFolderAsync(OpfsPath root, OpfsPath source, OpfsPath destination, OpfsFolderMoveOptions options, CancellationToken cancellationToken = default) => OpfsFolderOperations.MoveFolderAsync(root, source, destination, options, cancellationToken);

    /// <inheritdoc />
    public Task FinishPendingFolderMovesAsync(OpfsPath root, OpfsFolderMoveOptions options, CancellationToken cancellationToken = default) => OpfsFolderOperations.FinishPendingFolderMovesAsync(root, options, cancellationToken);
}
