namespace BlazorBasics.Opfs.Exceptions;

/// <summary>A file or a folder is not there (<c>NotFoundError</c>).</summary>
public sealed class OpfsNotFoundException : OpfsException
{
    /// <summary>Creates the exception.</summary>
    public OpfsNotFoundException(string message, string? browserErrorName = null, Exception? innerException = null)
        : base(message, browserErrorName, innerException)
    {
    }
}
