using System.Runtime.InteropServices.JavaScript;

namespace BlazorBasics.Opfs.TestApp;

/// <summary>The real lock for <c>back-forward.js</c>, which takes it, leaves the page and asks about it on coming back.</summary>
public static partial class BackForwardLockProbe
{
    public const string PAGE_QUERY = "run";

    private const string LOCK_NAME = "back-forward";

    private static readonly OpfsLocks Locks = new();
    private static IOpfsLock? Held;

    [JSExport]
    public static async Task<bool> AcquireAsync()
    {
        Held = await Locks.TryAcquireAsync(LOCK_NAME, OpfsLockMode.Exclusive);
        return Held is not null;
    }

    [JSExport]
    public static bool HasHolder() => Held is not null;

    [JSExport]
    public static bool IsHeld() => Held is { IsHeld: true };

    [JSExport]
    public static bool IsLost() => Held is { Lost.IsCancellationRequested: true };

    [JSExport]
    public static async Task<bool> CanTakeTheNameAsync()
    {
        using IOpfsLock? second = await Locks.TryAcquireAsync(LOCK_NAME, OpfsLockMode.Exclusive);
        return second is not null;
    }
}
