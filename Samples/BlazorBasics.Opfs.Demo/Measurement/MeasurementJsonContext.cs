using System.Text.Json.Serialization;

namespace BlazorBasics.Opfs.Demo.Measurement;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(CameraSetup))]
[JsonSerializable(typeof(RecorderOutcome))]
[JsonSerializable(typeof(FileCheck))]
internal sealed partial class MeasurementJsonContext : JsonSerializerContext;
