using System.Text.Json.Serialization;

namespace BlazorBasics.Opfs.Interop;

/// <summary>A storage estimate as <c>opfs.js</c> reports it.</summary>
internal sealed record OpfsEstimateRecord(
    [property: JsonPropertyName("usage")] double Usage,
    [property: JsonPropertyName("quota")] double Quota);
