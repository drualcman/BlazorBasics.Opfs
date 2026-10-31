namespace BlazorBasics.Opfs.Models;

/// <summary>How a download ended. Going over the limit is an expected outcome, not an exception.</summary>
/// <param name="Bytes">The bytes written, or zero when it was too large.</param>
/// <param name="TooLarge">True when the limit was exceeded and the file stayed as it was.</param>
public sealed record OpfsFetchResult(long Bytes, bool TooLarge);
