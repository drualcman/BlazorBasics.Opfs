namespace BlazorBasics.Opfs.Services;

/// <summary>Writes files. Each one is a swap the browser makes when the write closes.</summary>
[SupportedOSPlatform("browser")]
internal static class OpfsFileWriter
{
    private const int CHUNK_LENGTH = 1024 * 1024;

    public static Task WriteBytesAsync(OpfsPath file, ReadOnlyMemory<byte> content, CancellationToken cancellationToken) =>
        OpfsCall.RunAsync(() => OpfsWriteInterop.WriteBytesAsync(OpfsContent.NamesOf(file), OpfsContent.SegmentOf(content)), cancellationToken);

    public static Task WriteTextAsync(OpfsPath file, string content, CancellationToken cancellationToken) =>
        OpfsCall.RunAsync(() => OpfsWriteInterop.WriteTextAsync(OpfsContent.NamesOf(file), content), cancellationToken);

    public static async Task WriteStreamAsync(OpfsPath file, Stream content, CancellationToken cancellationToken)
    {
        await using IOpfsWriter writer = await OpenWriterAsync(file, false, cancellationToken);
        byte[] buffer = new byte[CHUNK_LENGTH];
        int read = await content.ReadAsync(buffer, cancellationToken);
        while (read > 0)
        {
            await writer.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            read = await content.ReadAsync(buffer, cancellationToken);
        }
        await writer.CompleteAsync(cancellationToken);
    }

    public static Task<IOpfsWriter> OpenWriterAsync(OpfsPath file, bool keepExistingData, CancellationToken cancellationToken) =>
        OpfsCall.RunAsync<IOpfsWriter>(async () =>
            new OpfsWriter(await OpfsWriteInterop.OpenWriterAsync(OpfsContent.NamesOf(file), keepExistingData)), cancellationToken);

    public static Task AppendAsync(OpfsPath file, ReadOnlyMemory<byte> content, CancellationToken cancellationToken) =>
        OpfsCall.RunAsync(() => OpfsWriteInterop.AppendBytesAsync(OpfsContent.NamesOf(file), OpfsContent.SegmentOf(content)), cancellationToken);

    public static Task TruncateAsync(OpfsPath file, long length, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        return OpfsCall.RunAsync(() => OpfsWriteInterop.TruncateAsync(OpfsContent.NamesOf(file), length), cancellationToken);
    }
}
