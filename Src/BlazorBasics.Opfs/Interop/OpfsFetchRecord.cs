using System.Text.Json.Serialization;

namespace BlazorBasics.Opfs.Interop;

/// <summary>A download as <c>opfs.js</c> reports it.</summary>
internal sealed record OpfsFetchRecord(
    [property: JsonPropertyName("bytes")] double Bytes,
    [property: JsonPropertyName("tooLarge")] bool TooLarge);
