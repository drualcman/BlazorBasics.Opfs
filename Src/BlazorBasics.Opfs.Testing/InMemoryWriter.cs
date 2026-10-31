namespace BlazorBasics.Opfs.Testing;

/// <summary>A write that builds on the side and swaps in when it completes.</summary>
internal sealed class InMemoryWriter : IOpfsWriter
{
    private readonly InMemoryFile Target;
    private readonly Action? ForgetCreatedFile;
    private readonly MemoryStream Pending = new();
    private bool Finished;

    public InMemoryWriter(InMemoryFile target, bool keepExistingData, Action? forgetCreatedFile)
    {
        Target = target;
        ForgetCreatedFile = forgetCreatedFile;
        if (keepExistingData)
        {
            Pending.Write(target.Content);
        }
    }

    public Task WriteAsync(ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default)
    {
        EnsureOpen();
        cancellationToken.ThrowIfCancellationRequested();
        Pending.Write(content.Span);
        return Task.CompletedTask;
    }

    public Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        EnsureOpen();
        Target.Replace(Pending.ToArray());
        Finish();
        return Task.CompletedTask;
    }

    public Task AbortAsync(CancellationToken cancellationToken = default)
    {
        EnsureOpen();
        Discard();
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        if (!Finished)
        {
            Discard();
        }
        return ValueTask.CompletedTask;
    }

    private void Discard()
    {
        ForgetCreatedFile?.Invoke();
        Finish();
    }

    private void Finish()
    {
        Finished = true;
        Target.OpenWriters--;
    }

    private void EnsureOpen()
    {
        if (Finished)
        {
            throw new InvalidOperationException("This writer already completed or aborted.");
        }
    }
}
