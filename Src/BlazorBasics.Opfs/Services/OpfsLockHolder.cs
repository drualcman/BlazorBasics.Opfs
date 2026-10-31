namespace BlazorBasics.Opfs.Services;

/// <summary>A lock taken, which disposing releases once.</summary>
[SupportedOSPlatform("browser")]
internal sealed class OpfsLockHolder(JSObject holder) : IDisposable
{
    private bool Released;

    public void Dispose()
    {
        if (!Released)
        {
            Released = true;
            OpfsCoordinationInterop.ReleaseLock(holder);
            holder.Dispose();
        }
    }
}
