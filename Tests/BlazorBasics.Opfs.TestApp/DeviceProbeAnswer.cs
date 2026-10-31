namespace BlazorBasics.Opfs.TestApp;

/// <summary>What <c>ProbeAsync</c> answered, with the type and browser error of its failure.</summary>
public sealed record DeviceProbeAnswer(bool IsAvailable, string Reason, string? FailureType, string? BrowserError);
