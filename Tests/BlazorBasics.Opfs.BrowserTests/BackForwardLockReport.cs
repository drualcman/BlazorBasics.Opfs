namespace BlazorBasics.Opfs.BrowserTests;

/// <summary>What <c>back-forward.js</c> finds on coming back: <c>Restored</c> is false when the page loaded again instead.</summary>
internal sealed record BackForwardLockReport(bool Restored, bool HasHolder, bool IsHeld, bool IsLost, bool CanTakeTheName);
