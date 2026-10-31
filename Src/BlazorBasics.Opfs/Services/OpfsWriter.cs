namespace BlazorBasics.Opfs.Services;

/// <summary>A writable stream of the browser kept open across several chunks.</summary>
[SupportedOSPlatform("browser")]
internal sealed class OpfsWriter(JSObject target) : IOpfsWriter
{
    private bool Finished;

    public Task WriteAsync(ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default)
    {
        EnsureOpen();
        return OpfsCall.RunAsync(() => OpfsWriteInterop.WriteToWriterAsync(target, OpfsContent.SegmentOf(content)), cancellationToken);
    }

    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        EnsureOpen();
        await OpfsCall.RunAsync(() => OpfsWriteInterop.CompleteWriterAsync(target), cancellationToken);
        Finish();
    }

    public async Task AbortAsync(CancellationToken cancellationToken = default)
    {
        EnsureOpen();
        await OpfsCall.RunAsync(() => OpfsWriteInterop.AbortWriterAsync(target), cancellationToken);
        Finish();
    }

    public async ValueTask DisposeAsync()
    {
        if (!Finished)
        {
            await OpfsCall.RunAsync(() => OpfsWriteInterop.AbandonWriterAsync(target), CancellationToken.None);
            Finish();
        }
    }

    private void EnsureOpen()
    {
        if (Finished)
        {
            throw new InvalidOperationException("This writer already completed or aborted.");
        }
    }

    private void Finish()
    {
        Finished = true;
        target.Dispose();
    }
}
