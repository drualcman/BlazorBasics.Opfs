namespace BlazorBasics.Opfs.Demo.Measurement;

public sealed record FileCheck(
    int CheckedChunks,
    int MatchedChunks,
    bool Playable,
    bool ReachedEnd,
    double DurationSeconds,
    int Width,
    int Height,
    int DecodedFrames,
    string? Failure);
