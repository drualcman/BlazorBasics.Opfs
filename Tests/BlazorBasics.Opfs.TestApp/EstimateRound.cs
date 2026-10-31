namespace BlazorBasics.Opfs.TestApp;

/// <summary>The usage estimated once a file was written, and once it was removed.</summary>
public sealed record EstimateRound(long AfterWrite, long AfterDelete);
