namespace BlazorBasics.Opfs.Abstractions;

/// <summary>A write open across several chunks. Disposing it without completing aborts it.</summary>
public interface IOpfsWriter : IAsyncDisposable
{
    /// <summary>Adds a chunk. Nothing is visible to readers yet.</summary>
    Task WriteAsync(ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default);

    /// <summary>Makes everything written visible at once.</summary>
    Task CompleteAsync(CancellationToken cancellationToken = default);

    /// <summary>Discards what was written and leaves the file as it was.</summary>
    Task AbortAsync(CancellationToken cancellationToken = default);
}
