namespace BlazorBasics.Opfs.Models;

/// <summary>A file as a listing or a lookup found it.</summary>
/// <param name="Name">The name as stored, which goes back into <see cref="OpfsPath.Combine"/> as it is.</param>
/// <param name="Length">The length in bytes.</param>
/// <param name="ModifiedUtc">When it was last written.</param>
/// <param name="Text">The content, for names ending in the suffix the listing asked for; otherwise null.</param>
public sealed record OpfsFileInfo(string Name, long Length, DateTimeOffset ModifiedUtc, string? Text = null)
{
    /// <summary>The name as it was written, for a file created through <see cref="OpfsPath.FromSegments"/>.</summary>
    public string DecodedName => OpfsNames.Decode(Name);
}
