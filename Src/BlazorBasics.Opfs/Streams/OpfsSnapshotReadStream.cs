namespace BlazorBasics.Opfs.Streams;

/// <summary>Reads a snapshot from the start by slices, and frees it when disposed.</summary>
[SupportedOSPlatform("browser")]
internal sealed class OpfsSnapshotReadStream(IOpfsFileSnapshot snapshot, JSObject file) : OpfsAsyncReadStream
{
    private const int SLICE_LENGTH = 1024 * 1024;

    private long ReadPosition;

    public override bool CanSeek => false;

    public override long Length => snapshot.Length;

    public override long Position
    {
        get => ReadPosition;
        set => throw new NotSupportedException();
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        int read = 0;
        int wanted = (int)Math.Min(Math.Min(buffer.Length, SLICE_LENGTH), snapshot.Length - ReadPosition);
        if (wanted > 0)
        {
            byte[] slice = await snapshot.ReadRangeAsync(ReadPosition, wanted, cancellationToken);
            slice.CopyTo(buffer);
            read = slice.Length;
            ReadPosition += read;
        }
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            file.Dispose();
        }
        base.Dispose(disposing);
    }
}
