namespace BlazorBasics.Opfs.Testing;

/// <summary>A file as it was. Reading it after a later write fails, as in Chromium and Firefox.</summary>
internal sealed class InMemoryFileSnapshot(InMemoryFile file) : IOpfsFileSnapshot
{
    private readonly byte[] Content = file.Content;
    private readonly int Version = file.Version;

    public long Length => Content.Length;

    public Task<byte[]> ReadRangeAsync(long start, long length, CancellationToken cancellationToken = default)
    {
        if (file.Version != Version)
        {
            throw new OpfsException("The file changed after the snapshot was taken.", "NotReadableError");
        }
        return Task.FromResult(Slice(Content, start, length));
    }

    public Stream OpenRead() => new MemoryStream(Content, false);

    public void Dispose()
    {
    }

    internal static byte[] Slice(byte[] content, long start, long length)
    {
        int from = (int)Math.Min(start, content.Length);
        int to = (int)Math.Min(start + length, content.Length);
        return content[from..to];
    }
}
