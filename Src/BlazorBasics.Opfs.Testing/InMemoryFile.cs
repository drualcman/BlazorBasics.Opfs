namespace BlazorBasics.Opfs.Testing;

/// <summary>One file of the store. Each write replaces the content, as the browser's swap does.</summary>
internal sealed class InMemoryFile
{
    public byte[] Content { get; private set; } = [];

    public DateTimeOffset ModifiedUtc { get; private set; } = DateTimeOffset.UtcNow;

    public int Version { get; private set; }

    public int OpenWriters { get; set; }

    public void Replace(byte[] content)
    {
        Content = content;
        ModifiedUtc = DateTimeOffset.UtcNow;
        Version++;
    }
}
