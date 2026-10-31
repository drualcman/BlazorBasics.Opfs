namespace BlazorBasics.Opfs.Services;

/// <summary>Moves data while the bytes stay in JavaScript.</summary>
[SupportedOSPlatform("browser")]
internal static class OpfsTransfers
{
    public static Task<long> CopyAsync(OpfsPath from, OpfsPath to, CancellationToken cancellationToken) =>
        OpfsCall.RunAsync(async () =>
        {
            double copied = await OpfsOperation.RunAsync(
                operationId => OpfsTransferInterop.CopyAsync(operationId, OpfsContent.NamesOf(from), OpfsContent.NamesOf(to)), cancellationToken);
            return (long)copied;
        }, cancellationToken);

    public static Task MoveFileAsync(OpfsPath from, OpfsPath to, CancellationToken cancellationToken) =>
        OpfsCall.RunAsync(() => OpfsTransferInterop.MoveFileAsync(OpfsContent.NamesOf(from), OpfsContent.NamesOf(to)), cancellationToken);

    public static Task<OpfsFetchResult> FetchToAsync(Uri url, OpfsPath file, OpfsFetchOptions? options, CancellationToken cancellationToken)
    {
        OpfsFetchOptions fetchOptions = options ?? new OpfsFetchOptions();
        return OpfsCall.RunAsync(async () =>
        {
            string json = await OpfsOperation.RunAsync(operationId => OpfsTransferInterop.FetchToAsync(
                operationId, url.ToString(), OpfsContent.NamesOf(file), fetchOptions.MaxBytes, fetchOptions.Mode, fetchOptions.Cache, fetchOptions.Credentials), cancellationToken);
            OpfsFetchRecord fetched = JsonSerializer.Deserialize(json, OpfsJsonContext.Default.OpfsFetchRecord)!;
            return new OpfsFetchResult((long)fetched.Bytes, fetched.TooLarge);
        }, cancellationToken);
    }

    public static Task<string> CreateObjectUrlAsync(OpfsPath file, CancellationToken cancellationToken) =>
        OpfsCall.RunAsync(() => OpfsTransferInterop.ObjectUrlAsync(OpfsContent.NamesOf(file)), cancellationToken);

    public static void RevokeObjectUrl(string url)
    {
        OpfsCall.EnsureBrowser();
        OpfsTransferInterop.RevokeObjectUrl(url);
    }

    public static Task DownloadAsync(OpfsPath file, string suggestedFileName, CancellationToken cancellationToken) =>
        OpfsCall.RunAsync(() => OpfsTransferInterop.DownloadFileAsync(OpfsContent.NamesOf(file), suggestedFileName), cancellationToken);
}
