namespace BlazorBasics.Opfs.TestApp;

/// <summary>How the estimate followed a file. <c>Verdict</c> is <c>went-down</c>, <c>stayed-up</c> or <c>never-rose</c>.</summary>
public sealed record EstimateAnswer(long FileLength, long Quota, long Before, IReadOnlyList<EstimateRound> Rounds, long Settled, int SettledAfterSeconds, string Verdict);
