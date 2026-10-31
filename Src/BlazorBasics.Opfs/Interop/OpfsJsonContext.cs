using System.Text.Json.Serialization;

namespace BlazorBasics.Opfs.Interop;

/// <summary>Source generated reading of what <c>opfs.js</c> returns, so a trimmed application needs no reflection.</summary>
[JsonSerializable(typeof(OpfsFileRecord))]
[JsonSerializable(typeof(OpfsFileRecord[]))]
[JsonSerializable(typeof(OpfsFolderRecord))]
[JsonSerializable(typeof(OpfsFetchRecord))]
[JsonSerializable(typeof(OpfsEstimateRecord))]
internal sealed partial class OpfsJsonContext : JsonSerializerContext;
