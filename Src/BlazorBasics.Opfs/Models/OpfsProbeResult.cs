namespace BlazorBasics.Opfs.Models;

/// <summary>Whether this browser can really store files here.</summary>
/// <param name="IsAvailable">True when a test file was written, read back and removed.</param>
/// <param name="Reason">Why it is not available, or empty.</param>
/// <param name="Failure">The failure behind the answer, when there was one.</param>
public sealed record OpfsProbeResult(bool IsAvailable, string Reason, Exception? Failure = null);
