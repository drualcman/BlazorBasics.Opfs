namespace BlazorBasics.Opfs.Abstractions;

/// <summary>A lock taken. Disposing it releases it. The browser can also take it away: see <see cref="Lost"/>.</summary>
public interface IOpfsLock : IDisposable
{
    /// <summary>False once the lock is released or lost, when it no longer protects anything.</summary>
    bool IsHeld { get; }

    /// <summary>
    /// Cancelled when the browser takes the lock away, which WebKit does to a page that enters the back/forward cache.
    /// Another tab may hold it by then. Releasing the lock normally never cancels this.
    /// </summary>
    CancellationToken Lost { get; }
}
