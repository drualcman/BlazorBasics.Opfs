using System.Text.Json.Serialization;

namespace BlazorBasics.Opfs.Interop;

/// <summary>A folder as <c>opfs.js</c> reports it.</summary>
internal sealed record OpfsFolderRecord(
    [property: JsonPropertyName("files")] OpfsFileRecord[] Files,
    [property: JsonPropertyName("folders")] string[] Folders);
