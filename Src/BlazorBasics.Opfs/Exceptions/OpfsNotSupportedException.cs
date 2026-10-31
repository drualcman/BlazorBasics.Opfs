namespace BlazorBasics.Opfs.Exceptions;

/// <summary>The call ran where it cannot work: outside a browser, or in a worker for an operation that needs a page.</summary>
public sealed class OpfsNotSupportedException : OpfsException
{
    private const string EXPLANATION =
        "The Origin Private File System only exists with .NET running in the browser. This call ran on a server, most likely " +
        "during prerendering, in InteractiveServer, or in the first visit of InteractiveAuto. Use InteractiveWebAssembly and " +
        "call from OnAfterRenderAsync rather than OnInitializedAsync.";

    /// <summary>Creates the exception for a call outside a browser.</summary>
    public OpfsNotSupportedException()
        : base(EXPLANATION)
    {
    }

    /// <summary>Creates the exception for a browser that refused the call.</summary>
    public OpfsNotSupportedException(string message, string? browserErrorName, Exception? innerException)
        : base(message, browserErrorName, innerException)
    {
    }
}
