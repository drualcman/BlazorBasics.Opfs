namespace BlazorBasics.Opfs.BrowserTests;

/// <summary>How one test ended in the browser.</summary>
public sealed record BrowserTestOutcome(string Name, bool Passed, string? Failure);
