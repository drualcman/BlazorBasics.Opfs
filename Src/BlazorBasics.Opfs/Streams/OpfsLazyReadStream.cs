namespace BlazorBasics.Opfs.Streams;

/// <summary>
/// A file read by slices, only when it is really read. It reports its path, so that an application can recognise it and
/// go straight to the file in JavaScript: it is left unsealed for that kind of marker.
/// </summary>
public class OpfsLazyReadStream : OpfsAsyncReadStream
{
    private const int SLICE_LENGTH = 1024 * 1024;

    private readonly IOpfsFileSystem FileSystem;
    private readonly long FileLength;
    private long CurrentPosition;

    /// <summary>Creates the stream. Nothing is read yet.</summary>
    public OpfsLazyReadStream(IOpfsFileSystem fileSystem, OpfsPath path, long length)
    {
        FileSystem = fileSystem;
        Path = path;
        FileLength = length;
    }

    /// <summary>The file this stream reads.</summary>
    public OpfsPath Path { get; }

    /// <inheritdoc />
    public override bool CanSeek => true;

    /// <inheritdoc />
    public override long Length => FileLength;

    /// <inheritdoc />
    public override long Position
    {
        get => CurrentPosition;
        set => CurrentPosition = Math.Clamp(value, 0, FileLength);
    }

    /// <inheritdoc />
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        int read = 0;
        int wanted = (int)Math.Min(Math.Min(buffer.Length, SLICE_LENGTH), FileLength - CurrentPosition);
        if (wanted > 0)
        {
            byte[] slice = await FileSystem.ReadRangeAsync(Path, CurrentPosition, wanted, cancellationToken);
            slice.CopyTo(buffer);
            read = slice.Length;
            CurrentPosition += read;
        }
        return read;
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        Position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => CurrentPosition + offset,
            _ => FileLength + offset
        };
        return CurrentPosition;
    }
}
