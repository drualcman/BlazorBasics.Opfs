namespace BlazorBasics.Opfs.ContractTests;

/// <summary>Gives one chunk and then fails, as a dropped upload or a broken source would.</summary>
public sealed class FailingStream : Stream
{
    private bool GaveTheChunk;

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (GaveTheChunk)
        {
            throw new IOException("The source failed.");
        }
        GaveTheChunk = true;
        buffer.Span[0] = 1;
        return ValueTask.FromResult(1);
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
