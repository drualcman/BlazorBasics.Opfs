using System.Globalization;
using System.Text;

namespace BlazorBasics.Opfs.Demo.Measurement;

public static class RecordingReport
{
    private const double LATE_GAP_MS = 1500;
    private const double BEHIND_QUEUE_MS = 1000;

    public static string Build(RecordingRun run)
    {
        StringBuilder text = new();
        Line(text, "BlazorBasics.Opfs recording measurement");
        Line(text, $"When (UTC): {DateTime.UtcNow:u}");
        AppendSetup(text, run.Camera);
        AppendChunks(text, run);
        AppendRecorder(text, run);
        AppendFile(text, run);
        AppendEstimates(text, run.Estimates);
        AppendPerChunk(text, run.Chunks);
        return text.ToString();
    }

    private static void AppendSetup(StringBuilder text, CameraSetup? camera)
    {
        if (camera is not null)
        {
            Line(text, $"Browser: {camera.UserAgent}");
            Line(text, $"Device: {camera.Processors} processors, {Number(camera.MemoryGigabytes)} GB reported");
            Line(text, $"Track: {camera.Width}x{camera.Height} at {Number(camera.FrameRate)} fps, audio {camera.HasAudio}, wake lock {camera.WakeLock}");
            Line(text, $"Format: {camera.MimeType}");
        }
    }

    private static void AppendChunks(StringBuilder text, RecordingRun run)
    {
        List<ChunkSample> chunks = run.Chunks;
        int written = chunks.Count(chunk => chunk.Failure is null);
        int recorded = run.Recorder?.ChunksSeen ?? chunks.Count;
        List<double> writes = chunks.Select(chunk => chunk.WriteMs).Order().ToList();

        Line(text, string.Empty);
        Line(text, $"Chunks recorded: {recorded}, written: {written}, lost: {recorded - written}, empty: {run.Recorder?.EmptyChunks ?? 0}");
        Line(text, $"Recorder cadence: {chunks.Count(chunk => chunk.GapMs > LATE_GAP_MS)} chunks over {LATE_GAP_MS} ms after the previous one, longest gap: {Milliseconds(Largest(chunks, chunk => chunk.GapMs))}, largest chunk: {chunks.Select(chunk => chunk.Bytes).DefaultIfEmpty().Max():N0} bytes");
        Line(text, $"Delayed by the writer (queue over {BEHIND_QUEUE_MS} ms): {chunks.Count(chunk => chunk.QueueDelayMs > BEHIND_QUEUE_MS)}, longest queue: {Milliseconds(Largest(chunks, chunk => chunk.QueueDelayMs))}");
        Line(text, $"WriteAsync: mean {Milliseconds(writes.Count > 0 ? writes.Average() : 0)}, p95 {Milliseconds(Percentile95(writes))}, max {Milliseconds(writes.Count > 0 ? writes[^1] : 0)}");
        foreach (ChunkSample failed in chunks.Where(chunk => chunk.Failure is not null))
        {
            Line(text, $"Chunk {failed.Index} failed: {failed.Failure}");
        }
    }

    private static void AppendRecorder(StringBuilder text, RecordingRun run)
    {
        Line(text, string.Empty);
        Line(text, $"OpenWriterAsync: {Milliseconds(run.OpenWriterMs)}");
        if (run.Recorder is not null)
        {
            Line(text, $"Recorded for: {Milliseconds(run.Recorder.DurationMs)}");
            Line(text, $"Queue drained after stop in: {Milliseconds(run.Recorder.DrainMs)}");
            Line(text, $"Longest main thread stall: {Milliseconds(run.Recorder.LongestStallMs)}");
            foreach (string interruption in run.Recorder.Interruptions)
            {
                Line(text, $"Interruption: {interruption}");
            }
        }
        Line(text, $"CompleteAsync: {Milliseconds(run.CompleteMs)}");
        if (run.Failure is not null)
        {
            Line(text, $"Run failed: {run.Failure}");
        }
    }

    private static void AppendFile(StringBuilder text, RecordingRun run)
    {
        Line(text, string.Empty);
        Line(text, $"Final size: {run.FileBytes:N0} bytes, chunks add up to {run.Chunks.Sum(chunk => (long)chunk.Bytes):N0}");
        if (run.Check is not null)
        {
            FileCheck check = run.Check;
            Line(text, $"Bytes identical to the recording: {check.MatchedChunks} of {check.CheckedChunks} chunks");
            Line(text, $"Plays from the start: {check.Playable}, plays to the end: {check.ReachedEnd}");
            Line(text, $"Video: {check.Width}x{check.Height}, {Number(check.DurationSeconds)} s, {check.DecodedFrames} frames decoded in the check");
            if (check.Failure is not null)
            {
                Line(text, $"Check failed: {check.Failure}");
            }
        }
    }

    private static void AppendEstimates(StringBuilder text, List<EstimateSample> estimates)
    {
        Line(text, string.Empty);
        foreach (EstimateSample estimate in estimates)
        {
            Line(text, $"estimate() {estimate.Moment}: {estimate.Usage:N0} of {estimate.Quota:N0} bytes");
        }
    }

    private static void AppendPerChunk(StringBuilder text, List<ChunkSample> chunks)
    {
        Line(text, string.Empty);
        Line(text, "index,bytes,gapMs,queueMs,writeMs");
        foreach (ChunkSample chunk in chunks)
        {
            Line(text, $"{chunk.Index},{chunk.Bytes},{Number(chunk.GapMs)},{Number(chunk.QueueDelayMs)},{Number(chunk.WriteMs)}");
        }
    }

    private static double Largest(List<ChunkSample> chunks, Func<ChunkSample, double> value) =>
        chunks.Count > 0 ? chunks.Max(value) : 0;

    private static double Percentile95(List<double> sorted) =>
        sorted.Count > 0 ? sorted[(int)Math.Ceiling(sorted.Count * 0.95) - 1] : 0;

    private static string Milliseconds(double value) => $"{Number(value)} ms";

    private static string Number(double value) => value.ToString("0.0", CultureInfo.InvariantCulture);

    private static void Line(StringBuilder text, string line) => text.Append(line).Append('\n');
}
