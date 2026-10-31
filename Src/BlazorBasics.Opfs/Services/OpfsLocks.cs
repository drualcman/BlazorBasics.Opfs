namespace BlazorBasics.Opfs.Services;

/// <summary>The Web Locks of the browser.</summary>
[SupportedOSPlatform("browser")]
public sealed class OpfsLocks : IOpfsLocks
{
    /// <inheritdoc />
    public Task<IOpfsLock?> TryAcquireAsync(string name, OpfsLockMode mode, CancellationToken cancellationToken = default) =>
        OpfsCall.RunAsync<IOpfsLock?>(async () =>
        {
            string browserMode = mode == OpfsLockMode.Exclusive ? "exclusive" : "shared";
            JSObject? holder = await OpfsCoordinationInterop.TryAcquireLockAsync(name, browserMode);
            return holder is null ? null : new OpfsLockHolder(holder);
        }, cancellationToken);
}
