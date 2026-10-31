namespace BlazorBasics.Opfs.Services;

/// <summary>The storage manager of the browser.</summary>
[SupportedOSPlatform("browser")]
public sealed class OpfsStorageInfo : IOpfsStorageInfo
{
    /// <inheritdoc />
    public Task<OpfsStorageEstimate?> EstimateAsync(CancellationToken cancellationToken = default) =>
        OpfsCall.RunAsync(async () =>
        {
            string? json = await OpfsCoordinationInterop.EstimateAsync();
            OpfsEstimateRecord? estimate = json is null ? null : JsonSerializer.Deserialize(json, OpfsJsonContext.Default.OpfsEstimateRecord);
            return estimate is null ? null : new OpfsStorageEstimate((long)estimate.Usage, (long)estimate.Quota);
        }, cancellationToken);

    /// <inheritdoc />
    public Task<bool> IsPersistedAsync(CancellationToken cancellationToken = default) =>
        OpfsCall.RunAsync(OpfsCoordinationInterop.IsPersistedAsync, cancellationToken);

    /// <inheritdoc />
    public Task<bool> RequestPersistenceAsync(CancellationToken cancellationToken = default) =>
        OpfsCall.RunAsync(OpfsCoordinationInterop.RequestPersistenceAsync, cancellationToken);
}
