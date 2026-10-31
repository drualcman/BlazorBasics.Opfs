namespace BlazorBasics.Opfs.Testing;

public sealed partial class InMemoryOpfsFileSystem
{
    /// <inheritdoc />
    public Task WriteBytesAsync(OpfsPath file, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Replace(file, content.ToArray());
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task WriteTextAsync(OpfsPath file, string content, CancellationToken cancellationToken = default) =>
        WriteBytesAsync(file, System.Text.Encoding.UTF8.GetBytes(content), cancellationToken);

    /// <inheritdoc />
    public async Task WriteStreamAsync(OpfsPath file, Stream content, CancellationToken cancellationToken = default)
    {
        await using IOpfsWriter writer = await OpenWriterAsync(file, false, cancellationToken);
        byte[] buffer = new byte[81920];
        int read = await content.ReadAsync(buffer, cancellationToken);
        while (read > 0)
        {
            await writer.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            read = await content.ReadAsync(buffer, cancellationToken);
        }
        await writer.CompleteAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<IOpfsWriter> OpenWriterAsync(OpfsPath file, bool keepExistingData = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        bool existed = Store.FileOrNullCreatingFolders(file) is not null;
        InMemoryFile target = Store.Files.TryGetValue(file, out InMemoryFile? found) ? found : new InMemoryFile();
        Store.Files[file] = target;
        target.OpenWriters++;
        IOpfsWriter writer = new InMemoryWriter(target, keepExistingData, existed ? null : () => Store.Files.Remove(file));
        return Task.FromResult(writer);
    }

    /// <inheritdoc />
    public Task AppendAsync(OpfsPath file, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byte[] existing = Store.FileOrNullCreatingFolders(file)?.Content ?? [];
        Replace(file, [.. existing, .. content.Span]);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task TruncateAsync(OpfsPath file, long length, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        InMemoryFile found = Store.RequireFile(file);
        if (found.Content.Length > length)
        {
            found.Replace(found.Content[..(int)length]);
        }
        return Task.CompletedTask;
    }

    private void Replace(OpfsPath file, byte[] content)
    {
        InMemoryFile target = Store.FileOrNullCreatingFolders(file) ?? new InMemoryFile();
        target.Replace(content);
        Store.Files[file] = target;
    }
}
