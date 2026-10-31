namespace BlazorBasics.Opfs.Testing;

/// <summary>Locks in memory, with the sharing rules of Web Locks.</summary>
public sealed class InMemoryOpfsLocks : IOpfsLocks
{
    private readonly Dictionary<string, (OpfsLockMode Mode, int Holders)> Held = [];

    /// <inheritdoc />
    public Task<IDisposable?> TryAcquireAsync(string name, OpfsLockMode mode, CancellationToken cancellationToken = default)
    {
        bool isHeld = Held.TryGetValue(name, out (OpfsLockMode Mode, int Holders) current);
        bool granted = !isHeld || (mode == OpfsLockMode.Shared && current.Mode == OpfsLockMode.Shared);
        IDisposable? holder = null;
        if (granted)
        {
            Held[name] = (mode, current.Holders + 1);
            holder = new InMemoryLockHolder(() => Release(name));
        }
        return Task.FromResult(holder);
    }

    private void Release(string name)
    {
        (OpfsLockMode Mode, int Holders) current = Held[name];
        if (current.Holders == 1)
        {
            Held.Remove(name);
        }
        else
        {
            Held[name] = (current.Mode, current.Holders - 1);
        }
    }
}
