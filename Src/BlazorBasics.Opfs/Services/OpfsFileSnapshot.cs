namespace BlazorBasics.Opfs.Services;

/// <summary>A <c>File</c> of the browser, which reads slices of the file without loading it.</summary>
[SupportedOSPlatform("browser")]
internal sealed class OpfsFileSnapshot(JSObject file) : IOpfsFileSnapshot
{
    private bool OwnedByStream;

    public long Length { get; } = (long)OpfsFileInterop.LengthOfSnapshot(file);

    public Task<byte[]> ReadRangeAsync(long start, long length, CancellationToken cancellationToken = default) =>
        OpfsCall.RunAsync(async () =>
        {
            using JSObject bytes = await OpfsFileInterop.ReadSnapshotRangeAsync(file, start, start + length);
            return OpfsFileInterop.BytesOf(bytes);
        }, cancellationToken);

    public Stream OpenRead()
    {
        OwnedByStream = true;
        return new OpfsSnapshotReadStream(this, file);
    }

    public void Dispose()
    {
        if (!OwnedByStream)
        {
            file.Dispose();
        }
    }
}
