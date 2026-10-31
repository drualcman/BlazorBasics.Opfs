namespace BlazorBasics.Opfs.Exceptions;

/// <summary>Another writer or handle holds the file (<c>NoModificationAllowedError</c>).</summary>
public sealed class OpfsFileBusyException : OpfsException
{
    /// <summary>Creates the exception.</summary>
    public OpfsFileBusyException(string message, string? browserErrorName = null, Exception? innerException = null)
        : base(message, browserErrorName, innerException)
    {
    }
}
