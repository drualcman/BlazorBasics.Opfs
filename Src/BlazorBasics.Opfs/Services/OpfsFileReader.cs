namespace BlazorBasics.Opfs.Services;

/// <summary>Reads files.</summary>
[SupportedOSPlatform("browser")]
internal static class OpfsFileReader
{
    public static Task<OpfsFileInfo?> GetInfoOrNullAsync(OpfsPath file, CancellationToken cancellationToken) =>
        OpfsCall.RunAsync(async () =>
        {
            string? json = await OpfsFileInterop.FileInfoOrNullAsync(OpfsContent.NamesOf(file));
            return json is null ? null : JsonSerializer.Deserialize(json, OpfsJsonContext.Default.OpfsFileRecord)!.ToInfo();
        }, cancellationToken);

    public static Task<IOpfsFileSnapshot?> OpenSnapshotOrNullAsync(OpfsPath file, CancellationToken cancellationToken) =>
        OpfsCall.RunAsync<IOpfsFileSnapshot?>(async () =>
        {
            JSObject? snapshot = await OpfsFileInterop.SnapshotOrNullAsync(OpfsContent.NamesOf(file));
            return snapshot is null ? null : new OpfsFileSnapshot(snapshot);
        }, cancellationToken);

    public static Task<string> ReadTextAsync(OpfsPath file, CancellationToken cancellationToken) =>
        OpfsCall.RunAsync(() => OpfsFileInterop.ReadTextAsync(OpfsContent.NamesOf(file)), cancellationToken);

    public static Task<byte[]> ReadBytesAsync(OpfsPath file, CancellationToken cancellationToken) =>
        OpfsCall.RunAsync(async () =>
        {
            using JSObject bytes = await OpfsFileInterop.ReadBytesAsync(OpfsContent.NamesOf(file));
            return OpfsFileInterop.BytesOf(bytes);
        }, cancellationToken);

    public static Task<byte[]> ReadRangeAsync(OpfsPath file, long start, int length, CancellationToken cancellationToken) =>
        OpfsCall.RunAsync(async () =>
        {
            using JSObject bytes = await OpfsFileInterop.ReadRangeAsync(OpfsContent.NamesOf(file), start, start + length);
            return OpfsFileInterop.BytesOf(bytes);
        }, cancellationToken);
}
