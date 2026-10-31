namespace BlazorBasics.Opfs.Demo.Measurement;

public sealed record RecorderOutcome(
    double DurationMs,
    int ChunksSeen,
    int EmptyChunks,
    double LongestStallMs,
    double DrainMs,
    IReadOnlyList<string> Interruptions);
