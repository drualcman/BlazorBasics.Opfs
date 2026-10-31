namespace BlazorBasics.Opfs.Demo.Measurement;

public sealed record CameraSetup(
    string MimeType,
    int Width,
    int Height,
    double FrameRate,
    bool HasAudio,
    bool WakeLock,
    string UserAgent,
    int Processors,
    double MemoryGigabytes);
