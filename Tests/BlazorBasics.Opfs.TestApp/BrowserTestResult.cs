namespace BlazorBasics.Opfs.TestApp;

/// <summary>How one test ended, by <c>Class.Method</c>, with <c>[n]</c> for each data row of a theory.</summary>
public sealed record BrowserTestResult(string Name, bool Passed, string? Failure);
