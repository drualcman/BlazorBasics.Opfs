namespace BlazorBasics.Opfs.TestApp;

/// <summary>
/// A move with a writer open on one of its files: the error if it was refused, how completing that writer ended, and what
/// each name holds afterwards, null when the name is gone.
/// </summary>
public sealed record MoveAnswer(string WriterOn, string? MoveError, string? WriterError, string? Source, string? Destination);
