using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using BlazorBasics.Opfs.Abstractions;
using BlazorBasics.Opfs.Exceptions;
using BlazorBasics.Opfs.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorBasics.Opfs.Demo.Measurement;

public sealed class RecordingRun(IOpfsFileSystem files, IOpfsStorageInfo storage, IJSObjectReference module, Action changed)
{
    public const string Folder = "recording-measurement";
    private const int SECONDS_BETWEEN_ESTIMATES = 10;

    private readonly long StartedAt = Stopwatch.GetTimestamp();
    private IOpfsWriter? Writer;

    public List<ChunkSample> Chunks { get; } = [];
    public List<EstimateSample> Estimates { get; } = [];
    public CameraSetup? Camera { get; private set; }
    public RecorderOutcome? Recorder { get; private set; }
    public double OpenWriterMs { get; private set; }
    public double CompleteMs { get; private set; }
    public long FileBytes { get; private set; }
    public FileCheck? Check { get; private set; }
    public string? Failure { get; private set; }
    public string? VideoUrl { get; private set; }
    public double ElapsedSeconds => Stopwatch.GetElapsedTime(StartedAt).TotalSeconds;

    public async Task RunAsync(ElementReference preview, ElementReference playback)
    {
        try
        {
            await files.ClearFolderAsync(Folder);
            Camera = await ReadAsync("openCamera", MeasurementJsonContext.Default.CameraSetup, preview);
            await SampleEstimateAsync("before");
            await RecordAsync(preview);
            await module.InvokeVoidAsync("closeCamera");
            await SampleEstimateAsync("after complete");
            OpfsFileInfo? info = await files.GetInfoOrNullAsync(FilePath);
            FileBytes = info?.Length ?? 0;
            VideoUrl = await files.CreateObjectUrlAsync(FilePath);
            Check = await ReadAsync("checkFile", MeasurementJsonContext.Default.FileCheck, playback, VideoUrl);
        }
        catch (Exception exception) when (exception is OpfsException or JSException)
        {
            Failure = $"{exception.GetType().Name}: {exception.Message}";
            await module.InvokeVoidAsync("closeCamera");
        }
    }

    [JSInvokable]
    public async Task OnChunkAsync(byte[] bytes, int index, double gapMs, double queueDelayMs)
    {
        string? failure = null;
        long started = Stopwatch.GetTimestamp();
        try
        {
            await Writer!.WriteAsync(bytes);
        }
        catch (OpfsException exception)
        {
            failure = $"{exception.GetType().Name}: {exception.Message}";
        }
        Chunks.Add(new ChunkSample(index, bytes.Length, gapMs, queueDelayMs, Stopwatch.GetElapsedTime(started).TotalMilliseconds, failure));
        changed();
    }

    private static string FilePath => $"{Folder}/take.mp4";

    private async Task RecordAsync(ElementReference preview)
    {
        long opening = Stopwatch.GetTimestamp();
        await using IOpfsWriter writer = await files.OpenWriterAsync(FilePath);
        OpenWriterMs = Stopwatch.GetElapsedTime(opening).TotalMilliseconds;
        Writer = writer;

        using PeriodicTimer sampling = new(TimeSpan.FromSeconds(SECONDS_BETWEEN_ESTIMATES));
        Task samples = SampleDuringAsync(sampling);
        using DotNetObjectReference<RecordingRun> listener = DotNetObjectReference.Create(this);
        Recorder = await ReadAsync("record", MeasurementJsonContext.Default.RecorderOutcome, listener);
        sampling.Dispose();
        await samples;

        await SampleEstimateAsync("before complete");
        long completing = Stopwatch.GetTimestamp();
        await writer.CompleteAsync();
        CompleteMs = Stopwatch.GetElapsedTime(completing).TotalMilliseconds;
    }

    private async Task SampleDuringAsync(PeriodicTimer timer)
    {
        int seconds = 0;
        while (await timer.WaitForNextTickAsync())
        {
            seconds += SECONDS_BETWEEN_ESTIMATES;
            await SampleEstimateAsync($"during, {seconds} s");
        }
    }

    private async Task SampleEstimateAsync(string moment)
    {
        OpfsStorageEstimate? estimate = await storage.EstimateAsync();
        if (estimate is not null)
        {
            Estimates.Add(new EstimateSample(moment, estimate.Usage, estimate.Quota));
        }
    }

    private async Task<TResult> ReadAsync<TResult>(string function, JsonTypeInfo<TResult> type, params object?[] arguments)
    {
        string json = await module.InvokeAsync<string>(function, arguments);
        return JsonSerializer.Deserialize(json, type)!;
    }
}
