namespace BlazorBasics.Opfs.Services;

/// <summary>The Web Locks of the browser.</summary>
[SupportedOSPlatform("browser")]
public sealed class OpfsLocks : IOpfsLocks
{
    /// <inheritdoc />
    public Task<IDisposable?> TryAcquireAsync(string name, OpfsLockMode mode, CancellationToken cancellationToken = default) =>
        OpfsCall.RunAsync<IDisposable?>(async () =>
        {
            string browserMode = mode == OpfsLockMode.Exclusive ? "exclusive" : "shared";
            JSObject? holder = await OpfsCoordinationInterop.TryAcquireLockAsync(name, browserMode);
            return holder is null ? null : new OpfsLockHolder(holder);
        }, cancellationToken);
}
