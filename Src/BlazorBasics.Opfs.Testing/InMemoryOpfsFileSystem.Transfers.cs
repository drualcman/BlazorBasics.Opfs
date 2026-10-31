using System.Net.Http.Headers;

namespace BlazorBasics.Opfs.Testing;

public sealed partial class InMemoryOpfsFileSystem
{
    private const string OBJECT_URL_PREFIX = "blob:in-memory/";

    /// <inheritdoc />
    public async Task<long> CopyAsync(OpfsPath from, OpfsPath to, CancellationToken cancellationToken = default)
    {
        byte[] content = Store.RequireFile(from).Content;
        await WriteBytesAsync(to, content, cancellationToken);
        return content.Length;
    }

    /// <inheritdoc />
    public Task MoveFileAsync(OpfsPath from, OpfsPath to, CancellationToken cancellationToken = default)
    {
        InMemoryFile source = Store.RequireFile(from);
        if (from != to)
        {
            EnsureFree(from, source);
            InMemoryFile? destination = Store.FileOrNullCreatingFolders(to);
            if (destination is not null)
            {
                EnsureFree(to, destination);
            }
            Store.Files.Remove(from);
            Store.Files[to] = source;
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<OpfsFetchResult> FetchToAsync(Uri url, OpfsPath file, OpfsFetchOptions? options = null, CancellationToken cancellationToken = default)
    {
        long maxBytes = options?.MaxBytes ?? 0;
        using HttpResponseMessage response = await RequestAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new OpfsFetchException($"The download failed with status {(int)response.StatusCode}.", (int)response.StatusCode);
        }
        OpfsFetchResult result;
        if (maxBytes > 0 && DeclaredLength(response.Content.Headers) > maxBytes)
        {
            result = new OpfsFetchResult(0, true);
        }
        else
        {
            result = await StoreBodyAsync(response, file, maxBytes, cancellationToken);
        }
        return result;
    }

    /// <inheritdoc />
    public Task<string> CreateObjectUrlAsync(OpfsPath file, CancellationToken cancellationToken = default)
    {
        Store.RequireFile(file);
        return Task.FromResult($"{OBJECT_URL_PREFIX}{file}");
    }

    /// <inheritdoc />
    public void RevokeObjectUrl(string url)
    {
    }

    /// <inheritdoc />
    public Task DownloadAsync(OpfsPath file, string suggestedFileName, CancellationToken cancellationToken = default)
    {
        Store.RequireFile(file);
        return Task.CompletedTask;
    }

    private async Task<HttpResponseMessage> RequestAsync(Uri url, CancellationToken cancellationToken)
    {
        if (Fetcher is null)
        {
            throw new OpfsFetchException("No fetcher was set on the in-memory file system.", 0);
        }
        return await Fetcher(url, cancellationToken);
    }

    private static long DeclaredLength(HttpContentHeaders headers) => headers.ContentLength ?? 0;

    private async Task<OpfsFetchResult> StoreBodyAsync(HttpResponseMessage response, OpfsPath file, long maxBytes, CancellationToken cancellationToken)
    {
        await using IOpfsWriter writer = await OpenWriterAsync(file, false, cancellationToken);
        await using Stream body = await response.Content.ReadAsStreamAsync(cancellationToken);
        byte[] buffer = new byte[81920];
        long total = 0;
        bool tooLarge = false;
        int read = await ReadBodyAsync(body, buffer, cancellationToken);
        while (read > 0 && !tooLarge)
        {
            total += read;
            tooLarge = maxBytes > 0 && total > maxBytes;
            if (!tooLarge)
            {
                await writer.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                read = await ReadBodyAsync(body, buffer, cancellationToken);
            }
        }
        if (!tooLarge)
        {
            await writer.CompleteAsync(cancellationToken);
        }
        return new OpfsFetchResult(tooLarge ? 0 : total, tooLarge);
    }

    private static async Task<int> ReadBodyAsync(Stream body, byte[] buffer, CancellationToken cancellationToken)
    {
        int read;
        try
        {
            read = await body.ReadAsync(buffer, cancellationToken);
        }
        catch (IOException failure)
        {
            throw new OpfsFetchException($"The download failed with status 0: {failure.Message}", 0, failure);
        }
        return read;
    }
}
