namespace BlazorBasics.Opfs.Testing;

/// <summary>A lock taken, which disposing releases once, and which a test can take away.</summary>
internal sealed class InMemoryLockHolder(Action<InMemoryLockHolder> release) : IOpfsLock
{
    private readonly CancellationTokenSource LostSource = new();
    private bool Finished;

    public bool IsHeld => !Finished;

    public CancellationToken Lost => LostSource.Token;

    public void Dispose()
    {
        if (!Finished)
        {
            Finished = true;
            release(this);
        }
    }

    public void Lose()
    {
        if (!Finished)
        {
            Finished = true;
            LostSource.Cancel();
        }
    }
}
