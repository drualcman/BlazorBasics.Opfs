namespace BlazorBasics.Opfs.Models;

/// <summary>What the browser estimates about the space of this site.</summary>
/// <param name="Usage">The bytes used.</param>
/// <param name="Quota">The bytes available in all.</param>
public sealed record OpfsStorageEstimate(long Usage, long Quota);
