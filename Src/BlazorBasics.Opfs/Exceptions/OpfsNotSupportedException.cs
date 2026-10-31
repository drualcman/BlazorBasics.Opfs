namespace BlazorBasics.Opfs.Exceptions;

/// <summary>The call ran outside a browser, so there is no file system to reach.</summary>
public sealed class OpfsNotSupportedException : OpfsException
{
    private const string EXPLANATION =
        "The Origin Private File System only exists with .NET running in the browser. This call ran on a server, most likely " +
        "during prerendering, in InteractiveServer, or in the first visit of InteractiveAuto. Use InteractiveWebAssembly and " +
        "call from OnAfterRenderAsync rather than OnInitializedAsync.";

    /// <summary>Creates the exception.</summary>
    public OpfsNotSupportedException()
        : base(EXPLANATION)
    {
    }
}
