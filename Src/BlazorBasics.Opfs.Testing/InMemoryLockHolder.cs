namespace BlazorBasics.Opfs.Testing;

/// <summary>A lock taken, which disposing releases once.</summary>
internal sealed class InMemoryLockHolder(Action release) : IDisposable
{
    private bool Released;

    public void Dispose()
    {
        if (!Released)
        {
            Released = true;
            release();
        }
    }
}
