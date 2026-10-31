namespace BlazorBasics.Opfs.Tests;

/// <summary>A body that gives some bytes and then breaks, or hangs until it is cancelled.</summary>
internal sealed class FetchFixtureStream(int length, bool stalls) : MemoryStream(new byte[length])
{
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        int read = await base.ReadAsync(buffer, cancellationToken);
        if (read == 0 && stalls)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        if (read == 0)
        {
            throw new IOException("The connection was cut.");
        }
        return read;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
}
