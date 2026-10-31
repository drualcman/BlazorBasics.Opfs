namespace BlazorBasics.Opfs.TestApp;

/// <summary>What a snapshot read gave once the file was written: <c>Read</c>, or else the browser error.</summary>
public sealed record SnapshotReadAnswer(string Case, long SnapshotLength, string? Read, string? BrowserError);
