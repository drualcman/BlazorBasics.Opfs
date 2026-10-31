namespace BlazorBasics.Opfs.Demo.Measurement;

public sealed record ChunkSample(int Index, int Bytes, double GapMs, double QueueDelayMs, double WriteMs, string? Failure);
