namespace BlazorBasics.Opfs.BrowserTests;

/// <summary>What <c>back-forward-away.js</c> got when it asked for the lock of the page in the back/forward cache.</summary>
internal sealed record BackForwardAwayReport(bool Granted);
