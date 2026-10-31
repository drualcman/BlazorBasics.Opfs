using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;

namespace BlazorBasics.Opfs.TestApp;

/// <summary>What <c>device.js</c> asks of .NET, each answer as JSON.</summary>
public static partial class DeviceChecks
{
    private static readonly JsonSerializerOptions AnswerJson = new(JsonSerializerDefaults.Web);

    [JSExport]
    public static async Task<string> ProbeAsync()
    {
        OpfsProbeResult probe = await new OpfsFileSystem().ProbeAsync();
        string? browserError = (probe.Failure as OpfsException)?.BrowserErrorName;
        return Answer(new DeviceProbeAnswer(probe.IsAvailable, probe.Reason, probe.Failure?.GetType().Name, browserError));
    }

    [JSExport]
    public static async Task<string> RunBatteryAsync() => Answer(await ContractBatteryRun.RunBatteryAsync());

    [JSExport]
    public static async Task<string> MeasureEstimateAsync() => Answer(await new EstimateMeasurement().RunAsync());

    [JSExport]
    public static async Task<string> MeasureSnapshotsAsync() => Answer(await new SnapshotMeasurement().RunAsync());

    [JSExport]
    public static async Task<string> MeasureMovesAsync() => Answer(await new MoveMeasurement().RunAsync());

    private static string Answer<TAnswer>(TAnswer answer) => JsonSerializer.Serialize(answer, AnswerJson);
}
