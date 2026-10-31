namespace BlazorBasics.Opfs.Testing;

/// <summary>Storage answers a test sets.</summary>
public sealed class InMemoryOpfsStorageInfo : IOpfsStorageInfo
{
    /// <summary>What the next estimate answers.</summary>
    public OpfsStorageEstimate? Estimate { get; set; }

    /// <summary>Whether the storage counts as persisted.</summary>
    public bool IsPersisted { get; set; }

    /// <summary>Whether a request for persistence is granted.</summary>
    public bool GrantsPersistence { get; set; } = true;

    /// <inheritdoc />
    public Task<OpfsStorageEstimate?> EstimateAsync(CancellationToken cancellationToken = default) => Task.FromResult(Estimate);

    /// <inheritdoc />
    public Task<bool> IsPersistedAsync(CancellationToken cancellationToken = default) => Task.FromResult(IsPersisted);

    /// <inheritdoc />
    public Task<bool> RequestPersistenceAsync(CancellationToken cancellationToken = default)
    {
        IsPersisted = IsPersisted || GrantsPersistence;
        return Task.FromResult(IsPersisted);
    }
}
