namespace BlazorBasics.Opfs.Abstractions;

/// <summary>What the browser says about the storage of this site.</summary>
public interface IOpfsStorageInfo
{
    /// <summary>
    /// The usage and quota of the site, or null when the browser cannot tell. It is an estimate: WebKit has been seen to
    /// keep reporting space after the files are gone, so do not treat it as an exact measure.
    /// </summary>
    Task<OpfsStorageEstimate?> EstimateAsync(CancellationToken cancellationToken = default);

    /// <summary>Whether the browser promised not to evict this site's storage.</summary>
    Task<bool> IsPersistedAsync(CancellationToken cancellationToken = default);

    /// <summary>Asks the browser not to evict this site's storage, and returns whether it agreed.</summary>
    Task<bool> RequestPersistenceAsync(CancellationToken cancellationToken = default);
}
