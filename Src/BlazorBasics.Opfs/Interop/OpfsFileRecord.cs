using System.Text.Json.Serialization;

namespace BlazorBasics.Opfs.Interop;

/// <summary>A file as <c>opfs.js</c> reports it.</summary>
internal sealed record OpfsFileRecord(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("length")] long Length,
    [property: JsonPropertyName("modified")] double Modified,
    [property: JsonPropertyName("text")] string? Text)
{
    public OpfsFileInfo ToInfo() =>
        new(Name, Length, DateTimeOffset.FromUnixTimeMilliseconds((long)Modified), Text);
}
