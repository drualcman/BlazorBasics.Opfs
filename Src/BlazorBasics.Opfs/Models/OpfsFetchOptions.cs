namespace BlazorBasics.Opfs.Models;

/// <summary>How to download a URL to a file.</summary>
public sealed record OpfsFetchOptions
{
    /// <summary>The most bytes to accept; zero means no limit.</summary>
    public long MaxBytes { get; init; }

    /// <summary>The <c>mode</c> of the request.</summary>
    public string Mode { get; init; } = "cors";

    /// <summary>The <c>cache</c> of the request.</summary>
    public string Cache { get; init; } = "no-store";

    /// <summary>The <c>credentials</c> of the request.</summary>
    public string Credentials { get; init; } = "omit";
}
