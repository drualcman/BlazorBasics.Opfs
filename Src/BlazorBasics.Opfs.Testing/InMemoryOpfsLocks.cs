namespace BlazorBasics.Opfs.Testing;

/// <summary>Locks in memory, with the sharing rules of Web Locks.</summary>
public sealed class InMemoryOpfsLocks : IOpfsLocks
{
    private readonly Dictionary<string, (OpfsLockMode Mode, List<InMemoryLockHolder> Holders)> Held = [];

    /// <inheritdoc />
    public Task<IOpfsLock?> TryAcquireAsync(string name, OpfsLockMode mode, CancellationToken cancellationToken = default)
    {
        bool isHeld = Held.TryGetValue(name, out (OpfsLockMode Mode, List<InMemoryLockHolder> Holders) current);
        bool granted = !isHeld || (mode == OpfsLockMode.Shared && current.Mode == OpfsLockMode.Shared);
        IOpfsLock? holder = null;
        if (granted)
        {
            InMemoryLockHolder taken = new(released => Release(name, released));
            List<InMemoryLockHolder> holders = isHeld ? current.Holders : [];
            holders.Add(taken);
            Held[name] = (mode, holders);
            holder = taken;
        }
        return Task.FromResult(holder);
    }

    /// <summary>Takes every holder of a lock away, as WebKit does on entering the back/forward cache.</summary>
    public void Lose(string name)
    {
        if (Held.Remove(name, out (OpfsLockMode Mode, List<InMemoryLockHolder> Holders) current))
        {
            foreach (InMemoryLockHolder holder in current.Holders)
            {
                holder.Lose();
            }
        }
    }

    private void Release(string name, InMemoryLockHolder holder)
    {
        (OpfsLockMode Mode, List<InMemoryLockHolder> Holders) current = Held[name];
        current.Holders.Remove(holder);
        if (current.Holders.Count == 0)
        {
            Held.Remove(name);
        }
    }
}
