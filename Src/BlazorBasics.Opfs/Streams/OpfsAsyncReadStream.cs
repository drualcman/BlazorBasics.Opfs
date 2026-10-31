namespace BlazorBasics.Opfs.Streams;

/// <summary>What the read streams share: read only, and asynchronous only, because a browser promise cannot be waited on.</summary>
public abstract class OpfsAsyncReadStream : Stream
{
    /// <inheritdoc />
    public override bool CanRead => true;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException("Browser files can only be read asynchronously: use ReadAsync.");

    /// <inheritdoc />
    public override void Flush()
    {
    }

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
