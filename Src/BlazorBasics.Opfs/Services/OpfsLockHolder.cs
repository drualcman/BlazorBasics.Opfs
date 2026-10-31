namespace BlazorBasics.Opfs.Services;

/// <summary>A lock taken, which disposing releases once, and which notices when the browser takes it away.</summary>
[SupportedOSPlatform("browser")]
internal sealed class OpfsLockHolder : IOpfsLock
{
    private readonly JSObject Holder;
    private readonly CancellationTokenSource LostSource = new();
    private bool Released;

    public OpfsLockHolder(JSObject holder)
    {
        Holder = holder;
        _ = WatchForLossAsync(OpfsCoordinationInterop.WhenLockLostAsync(holder));
    }

    public bool IsHeld => !Released && !LostSource.IsCancellationRequested;

    public CancellationToken Lost => LostSource.Token;

    public void Dispose()
    {
        if (!Released)
        {
            Released = true;
            OpfsCoordinationInterop.ReleaseLock(Holder);
            Holder.Dispose();
        }
    }

    private async Task WatchForLossAsync(Task<bool> lockLost)
    {
        if (await lockLost && !Released)
        {
            LostSource.Cancel();
        }
    }
}
