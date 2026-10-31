namespace BlazorBasics.Opfs.Abstractions;

/// <summary>Web Locks: they clash in a tab and between the tabs of a site, and the browser frees them when a tab closes.</summary>
public interface IOpfsLocks
{
    /// <summary>Takes a lock if it is free right now, or returns null. Disposing the result releases it.</summary>
    Task<IOpfsLock?> TryAcquireAsync(string name, OpfsLockMode mode, CancellationToken cancellationToken = default);
}
